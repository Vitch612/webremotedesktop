<?php
$com = new COM("ApplicationLauncher.Launcher");
//$com->runprocess(str_replace("/","\\",substr($_SERVER["SCRIPT_FILENAME"],0,strrpos($_SERVER["SCRIPT_FILENAME"],"/")))."\bin\src\DesktopInteractServer\bin\Debug\DesktopInteractServer.exe",true);
$com->runprocess(str_replace("/","\\",substr($_SERVER["SCRIPT_FILENAME"],0,strrpos($_SERVER["SCRIPT_FILENAME"],"/")))."\bin\src\DesktopInteractServer\bin\Release\DesktopInteractServer.exe",true);

sleep(1);
header("Location: http://".($_SERVER["SERVER_ADDR"]=="::1"?"127.0.0.1":$_SERVER["SERVER_ADDR"]).":81/");
die();