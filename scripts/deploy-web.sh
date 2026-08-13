#!/usr/bin/env bash
#
# 웹 빌드를 떠서 gh-pages 에 올린다. Docs/Build.md 절차를 그대로 스크립트로 옮긴 것이다.
# 유니티가 설치된 사람이 쓴다. 유니티가 없으면 GitHub Actions 의 "웹 배포" 워크플로를 쓸 것.
#
#   ./scripts/deploy-web.sh              # dev 최신을 빌드해서 배포
#   ./scripts/deploy-web.sh --dry-run    # 빌드까지만 하고 push 하지 않는다
#   ./scripts/deploy-web.sh --branch main
#
# 환경변수로 덮어쓸 수 있다:
#   UNITY_BIN    유니티 실행 파일 (기본값은 ProjectVersion.txt 의 버전으로 유추)
#   BUILD_DIR    빌드용 클린 클론 위치 (기본값 ../wolfman-build)
#   PAGES_DIR    gh-pages 워크트리 위치 (기본값 ../wolfman-pages)
#
set -euo pipefail

BRANCH=dev
DRY_RUN=0

while [ $# -gt 0 ]; do
  case "$1" in
    --branch) BRANCH=$2; shift 2 ;;
    --dry-run) DRY_RUN=1; shift ;;
    -h|--help) sed -n '2,20p' "$0" | sed 's/^# \?//'; exit 0 ;;
    *) echo "모르는 인자: $1" >&2; exit 2 ;;
  esac
done

REPO=$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)
BUILD_DIR=${BUILD_DIR:-$(dirname "$REPO")/wolfman-build}
PAGES_DIR=${PAGES_DIR:-$(dirname "$REPO")/wolfman-pages}
ORIGIN=$(git -C "$REPO" remote get-url origin)

step() { printf '\n\033[1m[%s/5] %s\033[0m\n' "$1" "$2"; }
die()  { printf '\033[31m실패: %s\033[0m\n' "$1" >&2; exit 1; }

# ---------------------------------------------------------------- 1. 소스
step 1 "$BRANCH 클린 체크아웃"

# 작업 중인 워킹 트리에서 빌드하면 커밋되지 않은 변경이 라이브로 새어 나간다.
# 그래서 항상 별도 클론에서 뜬다.
if [ ! -d "$BUILD_DIR/.git" ]; then
  echo "빌드용 클론을 만든다: $BUILD_DIR"
  git clone --branch "$BRANCH" --single-branch "$ORIGIN" "$BUILD_DIR"
fi

git -C "$BUILD_DIR" fetch origin "$BRANCH"
git -C "$BUILD_DIR" reset --hard FETCH_HEAD
git -C "$BUILD_DIR" lfs pull            # 아트·오디오는 LFS 에 있다
REVISION=$(git -C "$BUILD_DIR" rev-parse --short HEAD)
echo "리비전: $REVISION"

# ---------------------------------------------------------------- 2. 설정
step 2 '빌드 설정 검증'

# ADR 0003 이 못박은 세 값이다. 하나라도 어긋나면 로더가 죽거나 캐시에 막힌다
SETTINGS=$BUILD_DIR/ProjectSettings/ProjectSettings.asset
check() {
  grep -qE "^ *$1: $2\$" "$SETTINGS" \
    || die "$1 이 기대값($2)이 아니다. ADR 0003 을 읽고 고칠 것"
  echo "  OK  $1: $2  # $3"
}
check webGLCompressionFormat 0      'Brotli'
check webGLDecompressionFallback 1  '브라우저가 직접 푼다 — 꺼져 있으면 Pages 에서 로더가 죽는다'
check webGLNameFilesAsHashes 1      '내용이 바뀌면 URL 이 바뀐다'
check playModeTestRunnerEnabled 0   '켜져 있으면 테스트 어셈블리가 플레이어에 딸려간다 (#108)'

# ---------------------------------------------------------------- 3. 빌드
step 3 'WebGL 빌드'

if [ -z "${UNITY_BIN:-}" ]; then
  VERSION=$(sed -n 's/^m_EditorVersion: //p' "$BUILD_DIR/ProjectSettings/ProjectVersion.txt")
  UNITY_BIN="/mnt/c/Program Files/Unity/Hub/Editor/$VERSION/Editor/Unity.exe"
  [ -x "$UNITY_BIN" ] || UNITY_BIN="/c/Program Files/Unity/Hub/Editor/$VERSION/Editor/Unity.exe"
fi
[ -x "$UNITY_BIN" ] || die "유니티를 찾지 못했다: $UNITY_BIN (UNITY_BIN 으로 지정할 것)"

# 유니티는 윈도우 프로그램이라 윈도우 경로를 줘야 한다 (WSL 에서 실행하는 경우)
winpath() { if command -v wslpath >/dev/null 2>&1; then wslpath -w "$1"; else echo "$1"; fi; }

rm -rf "$BUILD_DIR/Build/Web" "$BUILD_DIR/Build.log"
# -quit 은 붙이지 않는다. WebBuilder.Build 가 EditorApplication.Exit 로 스스로 끝낸다
"$UNITY_BIN" -batchmode -nographics \
  -projectPath "$(winpath "$BUILD_DIR")" \
  -buildTarget WebGL \
  -executeMethod WebBuilder.Build \
  -logFile "$(winpath "$BUILD_DIR/Build.log")" || true   # 종료코드는 로그로 판정한다

grep -q 'result=Succeeded' "$BUILD_DIR/Build.log" \
  || die "빌드 실패. CS 에러 $(grep -c 'error CS' "$BUILD_DIR/Build.log" || true) 개 — $BUILD_DIR/Build.log 확인"
grep '\[WebBuilder\]' "$BUILD_DIR/Build.log" | sed 's/^/  /'

WEB=$BUILD_DIR/Build/Web
[ -f "$WEB/index.html" ] || die "index.html 이 없다: $WEB"

# index.html 이 가리키는 해시 파일이 실제로 있는지 본다. 없으면 브라우저에서 로더가 죽는다
for f in $(grep -oE '[a-f0-9]{32}\.[a-z.]+' "$WEB/index.html" | sort -u); do
  [ -f "$WEB/Build/$f" ] || die "index.html 이 참조하는 $f 가 산출물에 없다"
done

if [ "$DRY_RUN" = 1 ]; then
  printf '\n--dry-run 이라 여기서 멈춘다. 산출물: %s\n' "$WEB"
  exit 0
fi

# ---------------------------------------------------------------- 4. 배포
step 4 'gh-pages 비우고 채우기'

if [ ! -d "$PAGES_DIR" ]; then
  git -C "$BUILD_DIR" fetch origin gh-pages:gh-pages
  git -C "$BUILD_DIR" worktree add "$PAGES_DIR" gh-pages
fi
git -C "$PAGES_DIR" fetch origin gh-pages
git -C "$PAGES_DIR" reset --hard FETCH_HEAD

# 해시 파일명을 쓰므로 덮어쓰기가 아니라 "비우고 채우기" 여야 한다.
# 덮어쓰면 옛 빌드의 해시 파일이 지워지지 않고 영원히 쌓인다 (ADR 0003)
( cd "$PAGES_DIR" && find . -mindepth 1 -maxdepth 1 ! -name '.git' ! -name '.nojekyll' -exec rm -rf {} + )
cp -r "$WEB/." "$PAGES_DIR/"
touch "$PAGES_DIR/.nojekyll"   # 없으면 Pages 의 Jekyll 이 _ 로 시작하는 파일을 무시한다

if [ -z "$(git -C "$PAGES_DIR" status --porcelain)" ]; then
  echo '라이브와 같은 산출물이다. 배포할 것이 없다.'
  exit 0
fi

git -C "$PAGES_DIR" add -A
git -C "$PAGES_DIR" commit -m "deploy: ${BRANCH}@${REVISION} 빌드 배포"
git -C "$PAGES_DIR" push origin gh-pages

# ---------------------------------------------------------------- 5. 확인
step 5 '라이브 확인'

U=https://sonseongoh.github.io/wolfman
echo 'Pages 게시까지 1분쯤 걸린다. 기다린다...'
for _ in $(seq 1 30); do
  sleep 10
  live=$(curl -s "$U/" | grep -oE '[a-f0-9]{32}\.[a-z.]+' | sort -u)
  local_files=$(grep -oE '[a-f0-9]{32}\.[a-z.]+' "$WEB/index.html" | sort -u)
  if [ "$live" = "$local_files" ]; then
    echo "게시 확인됨"
    for f in $local_files; do
      printf '  %s  %s\n' "$(curl -s -o /dev/null -w '%{http_code}' "$U/Build/$f")" "$f"
    done
    printf '\n배포 완료: %s\n' "$U/"
    printf '브라우저에서 타이틀 → 마을 → 달 공개 → 사냥까지 한 바퀴 밟아볼 것.\n'
    printf '로더가 죽는 문제는 이때만 드러난다.\n'
    exit 0
  fi
done

echo "게시가 아직 반영되지 않았다. 잠시 뒤 $U/ 를 직접 확인할 것." >&2
