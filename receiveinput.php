<?php
include "comobject.php";
$op=$_REQUEST["action"];
if ($op=="click") {
  $p=$com->mouseclickl((int)$_REQUEST["x"],(int)$_REQUEST["y"]);
}
if ($op=="clickr") {
  $p=$com->mouseclickr((int)$_REQUEST["x"],(int)$_REQUEST["y"]);
}
if ($op=="clickd") {
  $p=$com->mousedblclickl((int)$_REQUEST["x"],(int)$_REQUEST["y"]);
}
if ($op=="clickrd") {
  $p=$com->mousedblclickl((int)$_REQUEST["x"],(int)$_REQUEST["y"]);
}
if ($op=="mute") {
  $p=$com->mute();
}
if ($op=="voldown") {
  $p=$com->volumedown();
}
if ($op=="volup") {
  $p=$com->volumeup();
}


  


