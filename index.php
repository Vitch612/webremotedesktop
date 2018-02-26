<?php
$useragent=$_SERVER['HTTP_USER_AGENT'];
$mobile=false;
if(preg_match('/(android|bb\d+|meego).+mobile|avantgo|bada\/|blackberry|blazer|compal|elaine|fennec|hiptop|iemobile|ip(hone|od)|iris|kindle|lge |maemo|midp|mmp|netfront|opera m(ob|in)i|palm( os)?|phone|p(ixi|re)\/|plucker|pocket|psp|series(4|6)0|symbian|treo|up\.(browser|link)|vodafone|wap|windows (ce|phone)|xda|xiino/i',$useragent)||preg_match('/1207|6310|6590|3gso|4thp|50[1-6]i|770s|802s|a wa|abac|ac(er|oo|s\-)|ai(ko|rn)|al(av|ca|co)|amoi|an(ex|ny|yw)|aptu|ar(ch|go)|as(te|us)|attw|au(di|\-m|r |s )|avan|be(ck|ll|nq)|bi(lb|rd)|bl(ac|az)|br(e|v)w|bumb|bw\-(n|u)|c55\/|capi|ccwa|cdm\-|cell|chtm|cldc|cmd\-|co(mp|nd)|craw|da(it|ll|ng)|dbte|dc\-s|devi|dica|dmob|do(c|p)o|ds(12|\-d)|el(49|ai)|em(l2|ul)|er(ic|k0)|esl8|ez([4-7]0|os|wa|ze)|fetc|fly(\-|_)|g1 u|g560|gene|gf\-5|g\-mo|go(\.w|od)|gr(ad|un)|haie|hcit|hd\-(m|p|t)|hei\-|hi(pt|ta)|hp( i|ip)|hs\-c|ht(c(\-| |_|a|g|p|s|t)|tp)|hu(aw|tc)|i\-(20|go|ma)|i230|iac( |\-|\/)|ibro|idea|ig01|ikom|im1k|inno|ipaq|iris|ja(t|v)a|jbro|jemu|jigs|kddi|keji|kgt( |\/)|klon|kpt |kwc\-|kyo(c|k)|le(no|xi)|lg( g|\/(k|l|u)|50|54|\-[a-w])|libw|lynx|m1\-w|m3ga|m50\/|ma(te|ui|xo)|mc(01|21|ca)|m\-cr|me(rc|ri)|mi(o8|oa|ts)|mmef|mo(01|02|bi|de|do|t(\-| |o|v)|zz)|mt(50|p1|v )|mwbp|mywa|n10[0-2]|n20[2-3]|n30(0|2)|n50(0|2|5)|n7(0(0|1)|10)|ne((c|m)\-|on|tf|wf|wg|wt)|nok(6|i)|nzph|o2im|op(ti|wv)|oran|owg1|p800|pan(a|d|t)|pdxg|pg(13|\-([1-8]|c))|phil|pire|pl(ay|uc)|pn\-2|po(ck|rt|se)|prox|psio|pt\-g|qa\-a|qc(07|12|21|32|60|\-[2-7]|i\-)|qtek|r380|r600|raks|rim9|ro(ve|zo)|s55\/|sa(ge|ma|mm|ms|ny|va)|sc(01|h\-|oo|p\-)|sdk\/|se(c(\-|0|1)|47|mc|nd|ri)|sgh\-|shar|sie(\-|m)|sk\-0|sl(45|id)|sm(al|ar|b3|it|t5)|so(ft|ny)|sp(01|h\-|v\-|v )|sy(01|mb)|t2(18|50)|t6(00|10|18)|ta(gt|lk)|tcl\-|tdg\-|tel(i|m)|tim\-|t\-mo|to(pl|sh)|ts(70|m\-|m3|m5)|tx\-9|up(\.b|g1|si)|utst|v400|v750|veri|vi(rg|te)|vk(40|5[0-3]|\-v)|vm40|voda|vulc|vx(52|53|60|61|70|80|81|83|85|98)|w3c(\-| )|webc|whit|wi(g |nc|nw)|wmlb|wonu|x700|yas\-|your|zeto|zte\-/i',substr($useragent,0,4)))
$mobile=true;
echo '<html>
<head>
<style>
body {
  //background-color:black;
}
.controls {
  width:100%;
}
.message {
  display:none;
  position:absolute;
  top:0;
  left:0;
  width:30%;
  z-index:30;
  background-color:white;
}
.cvdiv {
  position: absolute;
  left: 50%;
  top: 0;
  -webkit-transform: translate(-50%, 0);
  transform: translate(-50%, 0);
  padding:0;
  margin:0;
}
.overlay {
  position:absolute;
  top:0;
  background-color:rgba(255, 255, 255, 0);
  z-index:15;
  overflow: hidden;
  display: block;
  margin:0;
  padding:0;
  width:100%;
  //border-color:black;
  //border-style:solid;
  //border-width:1px;
}
.first {
  position:relative;
  z-index:5;
  margin:0;
  padding:0;
}
.second {
  position:relative;
  z-index:5;
  margin:0;
  padding:0;
}
.content {
  position:relative; 
}

</style>
<script type="text/javascript" src="/windows/js/jquery.min.js"></script>
<script type="text/javascript" src="/windows/js/interactions.js"></script>
'.($mobile?'<script type="text/javascript" src="/windows/js/mobile.js"></script>':'').'
<script>

var maxwidth;
var maxheight; 
var displaywidth;
var displayheight; 
var timestamp;
var refreshrate=33;

function setmsg(text) {
  $(".message").html(text);
}

function drawpointer(prevx,prevy) {
  var c = $(".overlay")[0];
  var ctx = c.getContext("2d");
  var img = document.getElementById("mousepointer");
  ctx.clearRect(prevx,prevy,12,19);
  ctx.drawImage(img,mousedposx,mousedposy);
}

var mousedposx=0;
var mousedposy=0;
function drawmouse() {
  if (displaywidth!=null && homemousex!=null) {
    var prevx=mousedposx;
    var prevy=mousedposy;
    mousedposx=Math.round(homemousex*displaywidth/homescreenwidth);
    mousedposy=Math.round(homemousey*displayheight/homescreenheight);       
    //setmsg("home mouse: "+homemousex+","+homemousey+"<BR>display mouse: "+mousedposx+","+mousedposy+"<BR>"+homescreenwidth+"x"+homescreenheight+"<BR>"+displaywidth+"x"+displayheight+"<BR>"+$(".overlay")[0].width+"x"+$(".overlay")[0].height);
    if (prevx!=mousedposx || prevy!=mousedposy) {
      drawpointer(prevx,prevy);
    }    
  }  
  getmouse();
  setTimeout(drawmouse,50);
}

function redim() {
  maxwidth= window.innerWidth || document.documentElement.clientWidth || document.body.clientWidth;
  maxheight= window.innerHeight || document.documentElement.clientHeight || document.body.clientHeight;
}

$(document).ready(function() {
  $(".first").show();
  $(".second").hide();  
  timestamp=new Date().getTime();
  redim();
  $(".first").attr("src","/windows/img/screen.jpeg?w="+maxwidth+"&h="+maxheight+"&ts="+timestamp);  
  getserverscreensize();
  getmouse();
  setTimeout(drawmouse,50);
  $(".first").on("load",function() {
    var newts = new Date().getTime();
    if (newts-timestamp<refreshrate)
      newts=refreshrate-newts+timestamp
    else
      newts=0;    
    setTimeout(function() {
      timestamp=new Date().getTime();
      $(".first").show();
      $(".second").hide();
      displaywidth = $(".first")[0].clientWidth;
      displayheight = $(".first")[0].clientHeight;      
      $(".overlay").css("width",displaywidth);
      $(".overlay").css("height",displayheight);
      $(".cvdiv").css("width",displaywidth);
      $(".cvdiv").css("height",displayheight);
      if ($(".overlay")[0].width != displaywidth || $(".overlay")[0].height!=displayheight) {
        $(".overlay")[0].width=displaywidth;
        $(".overlay")[0].height=displayheight;
        drawpointer(mousedposx,mousedposy);
      }
      redim();
      $(".second").attr("src","/windows/img/screen.jpeg?w="+maxwidth+"&h="+maxheight+"&ts="+timestamp);
    },newts);    
  });
  
  $(".second").on("load",function() {
    var newts = new Date().getTime();
    if (newts-timestamp<refreshrate)
      newts=refreshrate-newts+timestamp
    else
      newts=0;
    setTimeout(function() {
      timestamp=new Date().getTime();
      $(".first").hide();
      $(".second").show();
      displaywidth = $(".second")[0].clientWidth;
      displayheight = $(".second")[0].clientHeight;
      $(".overlay").css("width",displaywidth);
      $(".overlay").css("height",displayheight);
      $(".cvdiv").css("width",displaywidth);
      $(".cvdiv").css("height",displayheight);
      if ($(".overlay")[0].width != displaywidth || $(".overlay")[0].height!=displayheight) {
        $(".overlay")[0].width=displaywidth;
        $(".overlay")[0].height=displayheight;
        drawpointer(mousedposx,mousedposy);
      }
      redim();
      $(".first").attr("src","/windows/img/screen.jpeg?w="+maxwidth+"&h="+maxheight+"&ts="+timestamp);
    },newts);
  });
  $(".overlay").click(function(e) {
    var offset = $(this).offset();
    var X = (e.pageX - offset.left);
    var Y = (e.pageY - offset.top);
    var hX=Math.round(homescreenwidth * X / displaywidth);
    var hY=Math.round(homescreenheight * Y / displayheight);
    //setmsg(homescreenwidth+"x"+homescreenheight+" --> "+displaywidth+"x"+displayheight+"<BR>canvas: "+$(".overlay")[0].width+"x"+$(".overlay")[0].height+"<BR>click: "+X+","+Y+"<BR>send: "+hX+","+hY);
    sendclick(hX,hY);
  });  
  
  $( ".mousebutton" ).change(function() {
    switch($(this).val()) {
      case "mouseleft":
        break;
      case "mouseright":
        break;
    }
  });
  
  $( ".mouseclick" ).change(function() {
    switch($(this).val()) {
      case "singleclick":
        break;
      case "doubleclick":
        break;
    }
  });

  $(".btnmute").click(function() {
    sendmute();
  });
  $(".btnvoldown").click(function() {
    sendvoldown();
  });
  $(".btnvolup").click(function() {
    sendvolup();
  });
});
</script>
</head>
<body style="padding:0;margin:0">
<div class="content" style="padding:0;margin:0;text-align:center;">
  <div class="message"></div>
  <div class="cvdiv">    
    <canvas class="overlay"></canvas>
    <img id="mousepointer" src="/windows/cursor.png" style="z-index:0;position:absolute;"/>
    <img class="first" src=""/><img class="second" src=""/>
  </div>
</div>
<div class="controls">
<input class="btnmute" type="button" value="&#128266;"/><BR>
<input class="btnvolup" type="button" value="&#x25B2;"/><BR>
<input class="btnvoldown" type="button" value="&#x25BC;"/><BR>
Mouse Button<BR>
<input class="mousebutton" type="radio" name="mousebutton" value="mouseleft" checked>left<BR>
<input class="mousebutton" type="radio" name="mousebutton" value="mouseright">right<BR>
Click<BR>
<input class="mouseclick" type="radio" name="mouseclick" value="singleclick" checked>Simple<BR>
<input class="mouseclick" type="radio" name="mouseclick" value="doubleclick">Double<BR>
</div>
</body>
</html>';
