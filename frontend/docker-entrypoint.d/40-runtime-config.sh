#!/bin/sh
# Writes the backend URL (env var VITE_API_BASE_URL) into config.js every time the container starts,
# so the URL can be changed in docker-compose.yml WITHOUT rebuilding the image.
cat > /usr/share/nginx/html/config.js <<CONFIG
window.__APP_CONFIG__ = { API_BASE_URL: "${VITE_API_BASE_URL:-http://localhost:8080}" };
CONFIG
echo "Runtime config written: API_BASE_URL=${VITE_API_BASE_URL:-http://localhost:8080}"
