<?php
include "serverinteract.php";
$op=$_REQUEST["action"];

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
if ($op=="sendtext") {
  send("txtsd".$_REQUEST["text"]);
}