<?php
$com = new COM("ApplicationLauncher.Launcher");
$com->runprocess(str_replace("/","\\",substr($_SERVER["SCRIPT_FILENAME"],0,strrpos($_SERVER["SCRIPT_FILENAME"],"/")))."/bin/WebDesktop.exe",true);
sleep(1);
$base=substr($_SERVER["PHP_SELF"],0,strpos($_SERVER["PHP_SELF"],"/",1));
$applicationfolder=substr($_SERVER["SCRIPT_FILENAME"],0,strrpos($_SERVER["SCRIPT_FILENAME"],"/"));
$config=file_get_contents("$applicationfolder/bin/files/config.ini", $use_include_path);
$port=substr($config,strpos($config,"Port=")+5,strpos($config,"\n",strpos($config,"Port=")+5)-strpos($config,"Port=")-6);
header("Location: http://".$_SERVER["SERVER_NAME"].":$port/");
die('<!doctype html>
<html lang="en">
<head>
<link rel="shortcut icon" href="'.$base.'/bin/files/favicon.ico" type="image/x-icon" />
</head>
<body></body></html>');