#!/usr/bin/env bash
set -euo pipefail

# Configura Gmail como servidor SMTP para la API local.
# La contraseña se escribe una sola vez y se guarda en .env; nunca se imprime
# ni forma parte del historial del shell.
REPO_ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
ENV_FILE="$REPO_ROOT/.env"

read -r -p "Correo de Gmail: " GMAIL
GMAIL="${GMAIL//[[:space:]]/}"

if [[ -z "$GMAIL" || "$GMAIL" != *@* ]]; then
  echo "El correo no es válido." >&2
  exit 1
fi

read -r -s -p "Contraseña de aplicación de Gmail (16 caracteres): " APP_PASSWORD
echo
APP_PASSWORD="${APP_PASSWORD//[[:space:]]/}"

if [[ -z "$APP_PASSWORD" ]]; then
  echo "La contraseña de aplicación es obligatoria." >&2
  exit 1
fi

if [[ ${#APP_PASSWORD} -ne 16 ]]; then
  echo "Aviso: Google normalmente genera contraseñas de aplicación de 16 caracteres."
  read -r -p "¿Continuar de todos modos? [s/N] " CONFIRMAR
  CONFIRMAR="$(printf '%s' "$CONFIRMAR" | tr '[:upper:]' '[:lower:]')"
  if [[ "$CONFIRMAR" != "s" ]]; then
    exit 1
  fi
fi

# smtp.gmail.com, 587, SSL y el remitente ya vienen por defecto en la API.
# Reemplazamos solo las credenciales para no tocar el resto del .env.
TMP_FILE="$(mktemp "$REPO_ROOT/.env.XXXXXX")"
trap 'rm -f "$TMP_FILE"' EXIT
chmod 600 "$TMP_FILE"

if [[ -f "$ENV_FILE" ]]; then
  grep -vE '^[[:space:]]*(export[[:space:]]+)?SMTP_(USER|PASSWORD)[[:space:]]*=|^#[[:space:]]*Gmail:' \
    "$ENV_FILE" > "$TMP_FILE" || true
fi

{
  printf '\n# Gmail: solo correo y contraseña de aplicación\n'
  printf 'SMTP_USER=%s\n' "$GMAIL"
  printf 'SMTP_PASSWORD=%s\n' "$APP_PASSWORD"
} >> "$TMP_FILE"

mv "$TMP_FILE" "$ENV_FILE"
chmod 600 "$ENV_FILE"
trap - EXIT

echo
echo "Gmail configurado para $GMAIL en:"
echo "  $ENV_FILE"
echo "Solo se guardaron SMTP_USER y SMTP_PASSWORD."
echo "smtp.gmail.com, puerto 587 y SSL ya vienen configurados."
echo
echo "Reinicia la API para que tome los cambios:"
echo "  cd Back && dotnet run"
