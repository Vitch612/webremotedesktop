<?php

  $client = stream_socket_client("tcp://127.0.0.1:8888", $errno, $errorMessage);
  if ($client == false) {
    if ($errno==10061) {
      $com = new COM("ApplicationLauncher.Launcher");
      $pid=$com->runprocess(str_replace("/","\\",substr($_SERVER["SCRIPT_FILENAME"],0,strrpos($_SERVER["SCRIPT_FILENAME"],"/")))."\bin\src\DesktopInteractServer\bin\Release\DesktopInteractServer.exe",true);
      $client = stream_socket_client("tcp://127.0.0.1:8888", $errno, $errorMessage);
      if ($client == false) {
        die();
      }
    } else {
      die();
    }      
  }
  fwrite($client, "gscrr");
  $revval=stream_get_contents($client);
  fclose($client);
  echo $revval;
