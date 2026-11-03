@echo off
setlocal
cd /d "%~dp0"

rem Fecha a versao aberta para liberar o .exe
taskkill /f /im TapBoard.exe >nul 2>&1

"%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe" /nologo /target:winexe /optimize+ /codepage:65001 /out:TapBoard.exe /win32icon:TapBoard.ico /resource:TapBoard.ico,TapBoard.ico /r:System.Windows.Forms.dll /r:System.Drawing.dll /r:System.Xml.dll /r:System.Core.dll /r:System.Web.Extensions.dll TapBoard.cs Configuracoes.cs Icones.cs
if errorlevel 1 (
    echo.
    echo Erro ao compilar. Veja as mensagens acima.
    pause
    exit /b 1
)

start "" TapBoard.exe
