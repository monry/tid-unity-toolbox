#!/usr/bin/env bash
#
# apply-game-name.sh
#
# テンプレートから複製したプロジェクトに対して手動実行し、
# プレースホルダを実際のゲーム名へ一括置換するスクリプト。
#
# 引数の <GameName> は PascalCase で受け取り、プレースホルダごとに
# 対応するケースへ変換して置換する:
#
#   __Game__  -> PascalCase        (例: CookieClicker)
#   __GAME__  -> UPPER_SNAKE_CASE  (例: COOKIE_CLICKER)
#   __game__  -> lower_snake_case  (例: cookie_clicker)
#   --GAME--  -> UPPER-KEBAB-CASE  (例: COOKIE-CLICKER)
#   --game--  -> lower-kebab-case  (例: cookie-clicker)
#
# 置換対象:
#   1. Assets/<placeholder>          ディレクトリ名（対の .meta も同時にリネーム）
#   2. Assets/ 以下の *.asmdef        ファイル名・ファイル内容（対の .meta も同時にリネーム）
#   3. ProjectSettings/ProjectSettings.asset            内容
#   4. ProjectSettings/CsprojModifierSettings.{json,asset} 内容
#
# さらに ProjectSettings.asset の companyName / productName を明示的に上書きする
# （Unity Hub がテンプレート元の値を引き継がないため）:
#   companyName -> Tokyo Information Design Professional University（固定）
#   productName -> <GameName>（PascalCase）
#
# Usage:
#   ./apply-game-name.sh <GameName>
#
# 例:
#   ./apply-game-name.sh CookieClicker
#
# <GameName> は asmdef 名・ルート名前空間・ディレクトリ名として使われるため、
# スペースや記号を含まない PascalCase（先頭大文字・英数字のみ）である必要がある。
#
set -euo pipefail

# ProjectSettings.asset へ明示設定する固定の会社名（Unity Hub が引き継がないため）。
readonly COMPANY_NAME='Tokyo Information Design Professional University'

# --- 引数チェック ----------------------------------------------------------

if [ "$#" -ne 1 ]; then
  echo "Usage: $(basename "$0") <GameName>" >&2
  exit 1
fi

readonly GAME_NAME="$1"

if [ -z "$GAME_NAME" ]; then
  echo "Error: ゲーム名が空です。" >&2
  exit 1
fi

# ディレクトリ名・asmdef 名・名前空間として安全に使えるよう、
# スペースや記号を含まない PascalCase（先頭大文字、以降は英数字のみ）に限定する。
if ! printf '%s' "$GAME_NAME" | grep -Eq '^[A-Z][A-Za-z0-9]*$'; then
  echo "Error: ゲーム名 '$GAME_NAME' は無効です。" >&2
  echo "       スペースや記号を含まない PascalCase で指定してください" >&2
  echo "       （先頭は大文字、以降は英数字のみ）。" >&2
  echo "       例: CookieClicker, SpaceShooter, Match3" >&2
  exit 1
fi

# --- ケース変換 ------------------------------------------------------------

# PascalCase の GAME_NAME を単語境界で分割し、空白区切りの単語列にする。
#   - 小文字/数字 のあとに 大文字 が続く箇所   (例: ...e|C...)
#   - 連続する大文字の末尾（頭字語の境界）     (例: HTTP|Server)
readonly WORDS="$(printf '%s' "$GAME_NAME" \
  | sed -E 's/([a-z0-9])([A-Z])/\1 \2/g; s/([A-Z]+)([A-Z][a-z])/\1 \2/g')"

readonly PASCAL_CASE="$GAME_NAME"
readonly UPPER_SNAKE_CASE="$(printf '%s' "$WORDS" | LC_ALL=C tr '[:lower:]' '[:upper:]' | tr ' ' '_')"
readonly LOWER_SNAKE_CASE="$(printf '%s' "$WORDS" | LC_ALL=C tr '[:upper:]' '[:lower:]' | tr ' ' '_')"
readonly UPPER_KEBAB_CASE="$(printf '%s' "$WORDS" | LC_ALL=C tr '[:lower:]' '[:upper:]' | tr ' ' '-')"
readonly LOWER_KEBAB_CASE="$(printf '%s' "$WORDS" | LC_ALL=C tr '[:upper:]' '[:lower:]' | tr ' ' '-')"

# プレースホルダ と 置換後文字列 を同じ添字で対応させる（macOS の bash 3.2 でも動くよう並列配列を使う）。
readonly PLACEHOLDERS=('__Game__'     '__GAME__'          '__game__'          '--GAME--'          '--game--')
readonly REPLACEMENTS=("$PASCAL_CASE" "$UPPER_SNAKE_CASE" "$LOWER_SNAKE_CASE" "$UPPER_KEBAB_CASE" "$LOWER_KEBAB_CASE")

# --- プロジェクトルートの特定 ----------------------------------------------

# 本スクリプトは <ProjectRoot>/Packages/jp.ac.tid.unity-toolbox/Scripts/ に置かれている前提。
# 実行時のカレントディレクトリに依存しないよう、スクリプト位置から 3 階層上をルートとする。
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
readonly SCRIPT_DIR
PROJECT_ROOT="$(cd "$SCRIPT_DIR/../../.." && pwd)"
readonly PROJECT_ROOT

readonly ASSETS_DIR="$PROJECT_ROOT/Assets"
readonly PROJECT_SETTINGS_DIR="$PROJECT_ROOT/ProjectSettings"

if [ ! -d "$ASSETS_DIR" ] || [ ! -d "$PROJECT_SETTINGS_DIR" ]; then
  echo "Error: プロジェクトルートを特定できませんでした: $PROJECT_ROOT" >&2
  echo "       Assets/ および ProjectSettings/ が見つかりません。" >&2
  exit 1
fi

# --- ヘルパー --------------------------------------------------------------

# 文字列がいずれかのプレースホルダを含むか判定する。
contains_placeholder() {
  local text="$1" ph
  for ph in "${PLACEHOLDERS[@]}"; do
    case "$text" in
      *"$ph"*) return 0 ;;
    esac
  done
  return 1
}

# 文字列中の各プレースホルダを、対応するケースの文字列へ置換して返す。
# bash 3.2 では ${var//"pat"/"rep"} のようにクオートを入れ子にするとクオート文字が
# リテラルとして残るため、中間変数を用いた非クオート形式で置換する
# （プレースホルダ・置換値ともにグロブ特殊文字を含まないため安全）。
substitute_placeholders() {
  local text="$1" i ph rep
  for i in "${!PLACEHOLDERS[@]}"; do
    ph="${PLACEHOLDERS[$i]}"
    rep="${REPLACEMENTS[$i]}"
    text="${text//$ph/$rep}"
  done
  printf '%s' "$text"
}

# ファイル内の各プレースホルダを、対応するケースの文字列へ置換する（in-place）。
# BSD sed / GNU sed 双方で動くよう、一時ファイル経由で書き換える。
# プレースホルダ・置換後文字列ともに sed の特殊文字を含まないことを前提とする
# （置換後文字列は PascalCase 入力から英数字・アンダースコア・ハイフンのみで生成される）。
replace_in_file() {
  local file="$1"
  if [ ! -f "$file" ]; then
    return 0
  fi
  local rel="${file#"$PROJECT_ROOT"/}"
  # いずれかのプレースホルダを含むか（`--` 始まりも拾えるよう grep -e を使う）。
  local found=0 i
  for i in "${!PLACEHOLDERS[@]}"; do
    if grep -q -e "${PLACEHOLDERS[$i]}" "$file"; then
      found=1
      break
    fi
  done
  if [ "$found" -eq 0 ]; then
    echo "  [skip   ] プレースホルダを含みません: $rel"
    return 0
  fi
  local sed_args=()
  for i in "${!PLACEHOLDERS[@]}"; do
    sed_args+=(-e "s/${PLACEHOLDERS[$i]}/${REPLACEMENTS[$i]}/g")
  done
  local tmp
  tmp="$(mktemp)"
  LC_ALL=C sed "${sed_args[@]}" "$file" > "$tmp"
  mv "$tmp" "$file"
  echo "  [updated] $rel"
}

# ProjectSettings.asset (YAML) の指定キーの行を、インデントを保ったまま value で上書きする。
# 現在値に依存せずキー単位で設定するため、Unity Hub が付けた既定値も確実に置き換えられる。
# value は会社名（空白を含む固定文字列）/ PascalCase 名のみを想定し、sed の特殊文字を含まない。
set_yaml_field() {
  local file="$1" key="$2" value="$3"
  if [ ! -f "$file" ]; then
    echo "  [warn   ] ファイルが存在しません: ${file#"$PROJECT_ROOT"/}" >&2
    return 0
  fi
  if ! grep -qE "^[[:space:]]*${key}:" "$file"; then
    echo "  [warn   ] キー '${key}' が見つかりません: ${file#"$PROJECT_ROOT"/}" >&2
    return 0
  fi
  local tmp
  tmp="$(mktemp)"
  LC_ALL=C sed -E "s|^([[:space:]]*)${key}:.*|\1${key}: ${value}|" "$file" > "$tmp"
  mv "$tmp" "$file"
  echo "  [set    ] ${key}: ${value}"
}

# ファイル / ディレクトリをリネームし、対になる .meta も同時にリネームする。
rename_with_meta() {
  local src="$1"
  local dst="$2"
  if [ ! -e "$src" ]; then
    return 0
  fi
  if [ -e "$dst" ]; then
    echo "  [skip   ] 既に存在するためリネームしません: ${dst#"$PROJECT_ROOT"/}" >&2
    return 0
  fi
  mv "$src" "$dst"
  echo "  [renamed] ${src#"$PROJECT_ROOT"/} -> ${dst#"$PROJECT_ROOT"/}"
  if [ -e "${src}.meta" ]; then
    mv "${src}.meta" "${dst}.meta"
    echo "  [renamed] ${src#"$PROJECT_ROOT"/}.meta -> ${dst#"$PROJECT_ROOT"/}.meta"
  fi
}

# --- 実行 ------------------------------------------------------------------

echo "プロジェクトルート: $PROJECT_ROOT"
echo "次のプレースホルダを置換します:"
for i in "${!PLACEHOLDERS[@]}"; do
  echo "  ${PLACEHOLDERS[$i]} -> ${REPLACEMENTS[$i]}"
done
echo

# 1 & 3. Assets/ 以下の *.asmdef: 内容を置換し、ファイル名に含まれる場合はリネーム。
echo "[1/4] *.asmdef を処理"
while IFS= read -r -d '' asmdef; do
  replace_in_file "$asmdef"
  base="$(basename "$asmdef")"
  if contains_placeholder "$base"; then
    dir="$(dirname "$asmdef")"
    new_base="$(substitute_placeholders "$base")"
    rename_with_meta "$asmdef" "$dir/$new_base"
  fi
done < <(find "$ASSETS_DIR" -type f -name '*.asmdef' -print0)
echo

# 4. ProjectSettings/ProjectSettings.asset の内容を置換し、companyName / productName を明示設定。
echo "[2/4] ProjectSettings.asset を処理"
project_settings="$PROJECT_SETTINGS_DIR/ProjectSettings.asset"
replace_in_file "$project_settings"
# Unity Hub はテンプレート元の companyName / productName を引き継がないため、ここで上書きする。
set_yaml_field "$project_settings" "companyName" "$COMPANY_NAME"
set_yaml_field "$project_settings" "productName" "$PASCAL_CASE"
echo

# 5. ProjectSettings/CsprojModifierSettings.{json,asset} の内容を置換。
#    実体は .json だが、将来 .asset 化された場合に備え両方を対象にする。
echo "[3/4] CsprojModifierSettings を処理"
csproj_modifier_found=0
for csproj_modifier in \
  "$PROJECT_SETTINGS_DIR/CsprojModifierSettings.json" \
  "$PROJECT_SETTINGS_DIR/CsprojModifierSettings.asset"; do
  if [ -f "$csproj_modifier" ]; then
    replace_in_file "$csproj_modifier"
    csproj_modifier_found=1
  fi
done
if [ "$csproj_modifier_found" -eq 0 ]; then
  echo "  [warn   ] CsprojModifierSettings ファイルが見つかりません。" >&2
fi
echo

# 2. Assets/<placeholder> ディレクトリをリネーム（配下の asmdef は処理済みなので最後に実施）。
echo "[4/4] Assets/ 直下のプレースホルダディレクトリを処理"
for i in "${!PLACEHOLDERS[@]}"; do
  rename_with_meta "$ASSETS_DIR/${PLACEHOLDERS[$i]}" "$ASSETS_DIR/${REPLACEMENTS[$i]}"
done
echo

echo "完了しました。Unity を開き直す（またはエディタにフォーカスして再インポート）してください。"
