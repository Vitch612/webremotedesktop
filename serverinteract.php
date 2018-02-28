<?php
function send($string) {
  $client = stream_socket_client("tcp://192.168.137.1:8888", $errno, $errorMessage);
  if ($client == false) {
    if ($errno==10061) {
      $com = new COM("ApplicationLauncher.Launcher");
      $pid=$com->runprocess(str_replace("/","\\",substr($_SERVER["SCRIPT_FILENAME"],0,strrpos($_SERVER["SCRIPT_FILENAME"],"/")))."\bin\src\DesktopInteractServer\bin\Release\DesktopInteractServer.exe",true);
      if ($pid!=-1) {
        $client = stream_socket_client("tcp://192.168.137.1:8888", $errno, $errorMessage);
        if ($client == false) {
          die();
        }        
      } else {
        die();
      }
    } else {
      die();
    }      
  }
  fwrite($client, $string);
  $revval=stream_get_contents($client);
  fclose($client);
  return $revval;
}



