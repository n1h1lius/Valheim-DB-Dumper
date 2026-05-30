@echo off
title Sacred Ledger - Starting Server...
echo Starting local server for Sacred Ledger...
echo DO NOT CLOSE THIS WINDOW while using the web application.

:: Check if Python is installed
where python >nul 2>nul
if %errorlevel% neq 0 (
    echo Error: Python is not installed or not in the PATH.
    echo Please, install Python from python.org
    pause
    exit
)

:: Open browser automatically (default)
start http://localhost:8000

:: Launch server on port 8000
python -m http.server 8000
