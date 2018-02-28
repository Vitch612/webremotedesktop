<?php
include "serverinteract.php";
/*
function logrequest($info) {
  $myfile = fopen("logfile.txt", "a");
  fwrite($myfile, $info);
  fclose($myfile);
}
*/
$op=$_REQUEST["action"];
if ($op=="sendtext") {
  send("txtsd".$_REQUEST["text"]);    
}
if ($op=="click") {
  send("smcll".$_REQUEST["x"].",".$_REQUEST["y"]);
}
if ($op=="clickr") {
  send("smclr".$_REQUEST["x"].",".$_REQUEST["y"]);
}
if ($op=="clickd") {
  send("smdcl".$_REQUEST["x"].",".$_REQUEST["y"]);
}
if ($op=="clickrd") {
  send("smdcr".$_REQUEST["x"].",".$_REQUEST["y"]);
}
if ($op=="mute") {
  send("svmut");
}
if ($op=="voldown") {
  send("svodo");
}
if ($op=="volup") {
  send("svoup");
}
if ($op=="moused") {
  //logrequest("mouse down: ".$_REQUEST["x"].",".$_REQUEST["y"]."\n");
  send("mousd".$_REQUEST["x"].",".$_REQUEST["y"]);
}
if ($op=="mouseu") {
  //logrequest("mouse up: ".$_REQUEST["x"].",".$_REQUEST["y"]."\n");
  send("mousu".$_REQUEST["x"].",".$_REQUEST["y"]);
}
if ($op=="mousem") {
  //logrequest("mouse move: ".$_REQUEST["x"].",".$_REQUEST["y"]."\n");
  send("mousm".$_REQUEST["x"].",".$_REQUEST["y"]);  
}
