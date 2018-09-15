
@echo off
@echo //This file was automatically generated with a batch file with .glsl files as a source  > C:\Users\Micha\Desktop\GLDrawer\Gl3DrawerCLR\OpenGL3\shaders.h
@echo //Both the batch file and shader used can be found in the bin/Shaders folder of the projects working directory  >> C:\Users\Micha\Desktop\GLDrawer\Gl3DrawerCLR\OpenGL3\shaders.h
@echo #pragma once  >> C:\Users\Micha\Desktop\GLDrawer\Gl3DrawerCLR\OpenGL3\shaders.h
@echo const char * PolygonFrag = >> C:\Users\Micha\Desktop\GLDrawer\Gl3DrawerCLR\OpenGL3\shaders.h

SETLOCAL DisableDelayedExpansion
FOR /F "usebackq delims=" %%a in (`"findstr /n ^^ PolygonFragment.glsl"`) do (
    set "var=%%a"
    SETLOCAL EnableDelayedExpansion
    set "var=!var:*:=!"

	 echo(!var!
	set /p "="^""" <nul >> C:\Users\Micha\Desktop\GLDrawer\Gl3DrawerCLR\OpenGL3\shaders.h
	IF not [!var!] == [] 	set /p "=!var!" <nul >> C:\Users\Micha\Desktop\GLDrawer\Gl3DrawerCLR\OpenGL3\shaders.h
	@echo \n^" >> C:\Users\Micha\Desktop\GLDrawer\Gl3DrawerCLR\OpenGL3\shaders.h

    ENDLOCAL
)
@echo ; >> C:\Users\Micha\Desktop\GLDrawer\Gl3DrawerCLR\OpenGL3\shaders.h

@echo const char * RectVert = >> C:\Users\Micha\Desktop\GLDrawer\Gl3DrawerCLR\OpenGL3\shaders.h

SETLOCAL DisableDelayedExpansion
FOR /F "usebackq delims=" %%a in (`"findstr /n ^^ RectVertex.glsl"`) do (
    set "var=%%a"
    SETLOCAL EnableDelayedExpansion
    set "var=!var:*:=!"

	 echo(!var!
	set /p "="^""" <nul >> C:\Users\Micha\Desktop\GLDrawer\Gl3DrawerCLR\OpenGL3\shaders.h
	IF not [!var!] == [] 	set /p "=!var!" <nul >> C:\Users\Micha\Desktop\GLDrawer\Gl3DrawerCLR\OpenGL3\shaders.h
	@echo \n^" >> C:\Users\Micha\Desktop\GLDrawer\Gl3DrawerCLR\OpenGL3\shaders.h

    ENDLOCAL
)
@echo ; >> C:\Users\Micha\Desktop\GLDrawer\Gl3DrawerCLR\OpenGL3\shaders.h

@echo const char * TextureFrag = >> C:\Users\Micha\Desktop\GLDrawer\Gl3DrawerCLR\OpenGL3\shaders.h

SETLOCAL DisableDelayedExpansion
FOR /F "usebackq delims=" %%a in (`"findstr /n ^^ TextureFragment.glsl"`) do (
    set "var=%%a"
    SETLOCAL EnableDelayedExpansion
    set "var=!var:*:=!"

	 echo(!var!
	set /p "="^""" <nul >> C:\Users\Micha\Desktop\GLDrawer\Gl3DrawerCLR\OpenGL3\shaders.h
	IF not [!var!] == [] 	set /p "=!var!" <nul >> C:\Users\Micha\Desktop\GLDrawer\Gl3DrawerCLR\OpenGL3\shaders.h
	@echo \n^" >> C:\Users\Micha\Desktop\GLDrawer\Gl3DrawerCLR\OpenGL3\shaders.h

    ENDLOCAL
)
@echo ; >> C:\Users\Micha\Desktop\GLDrawer\Gl3DrawerCLR\OpenGL3\shaders.h

echo Finished!
timeout 1