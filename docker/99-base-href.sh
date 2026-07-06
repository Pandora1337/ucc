#!/bin/sh
set -e

BASE_HREF="${BASE_HREF:-}"

# Strip leading and trailing slashes
BASE_HREF="${BASE_HREF#/}"
BASE_HREF="${BASE_HREF%/}"

NGINX_DIR="/usr/share/nginx/html"
INDEX_FILE="$NGINX_DIR/index.html"

if [ -z $BASE_HREF ] || [ "$BASE_HREF" = "/" ]; then
  echo "Not changing base href..."
  exit 0
fi

if [ -f "$INDEX_FILE" ] && [ -w "$INDEX_FILE" ]; then
  sed -i 's|<base href="/" />|<base href="'"/${BASE_HREF}/"'" />|g' "$INDEX_FILE"
  echo "Set <base href=\"/${BASE_HREF}/\"> in index.html"
else
  echo "Warning: index.html not accessible at $INDEX_FILE"
  exit 1
fi

if [ -e "$NGINX_DIR" ] && [ -w "$NGINX_DIR" ]; then
  ln -s $NGINX_DIR $NGINX_DIR/$BASE_HREF
  echo "Linking to html root: ${NGINX_DIR}/${BASE_HREF}"
else
  echo "Warning: $NGINX_DIR not accessible"
  exit 1
fi
