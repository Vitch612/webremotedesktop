<?php
$com = new COM("screenshot.grabscreen");
$p=$com->getscreen_resolution();
echo $p[0].",".$p[1];


