#!/usr/env bash
set -euo pipefail

rm -f .env
name=$(xrandr --listactivemonitors | awk -F " " '{ printf("%s", $4) }') && echo "AVALONIA_SCREEN_SCALE_FACTORS='$name=2'" >> .env
