<?php
$com = new COM("screenshot.grabscreen");
$p=$com->getmousepos();
echo $p[0].",".$p[1];

