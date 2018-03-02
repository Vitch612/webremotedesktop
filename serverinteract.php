<?php
function send($string) {
  $fp = pfsockopen("tcp://127.0.0.1",8888,$errno,$errorMessage);
  if (!$fp) {
    if ($errno==10061) {
      $com = new COM("ApplicationLauncher.Launcher");
      $pid=$com->runprocess(str_replace("/","\\",substr($_SERVER["SCRIPT_FILENAME"],0,strrpos($_SERVER["SCRIPT_FILENAME"],"/")))."\bin\src\DesktopInteractServer\bin\Release\DesktopInteractServer.exe",true);
      if ($pid!=-1) {
        $fp = pfsockopen("tcp://127.0.0.1",8888,$errno,$errorMessage);        
        if (!$fp)
          die();
      } else
        die();
    } else
      die(); 
  } 
  fputs($fp,$string);
  $retval = "";
  while (!feof($fp)) {
    $retval .= fgets($fp, 1024);
  }
  fflush($fp);
  fclose($fp);
  return $retval;
}