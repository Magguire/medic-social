#!/bin/sh
set -eu

printf '%s\n' 'window.__MEDSOCIAL_CONFIG__ = { apiBaseUrl: window.location.origin };' > /app/public/runtime-config.js

exec "$@"
