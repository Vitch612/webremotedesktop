<?php

$op=$_REQUEST["action"];
$com = new COM("screenshot.grabscreen");
if ($op=="click") {
  $p=$com->mouseclickl((int)$_REQUEST["x"],(int)$_REQUEST["y"]);
}


  


