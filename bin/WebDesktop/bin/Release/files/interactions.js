var refreshrate=0;
var timeout=1000;
var maxwidth;
var maxheight;
var displaywidth;
var displayheight;
var timestamp;
var firefox;
var frames=new Array(20);
var frameptr=0;
var gottwenty=false;
var fps="NA";
var homemousex;
var homemousey;
var homescreenwidth;
var homescreenheight;
var mousedposx=0;
var mousedposy=0;
var isDragging = false;
var canceldrag = true;
var dragstep = 20;
var startx=0;
var starty=0;
var lastx=0;
var lasty=0;
mousegetrate = 50;
var mdown=false;

function getmouse() {
  var sendaction="getmouse";
  $.ajax({
    url: "/receiveinput",
    method: "GET",
    data: {action:sendaction}
    }).done(function(data) {
        var pos=data.split(",");
        homemousex=Number(pos[0]);
        homemousey=Number(pos[1]);
        drawmouse();
    });
}

function getserverscreensize() {
  var sendaction="getscreensize";
  $.ajax({
    url: "/receiveinput",
    method: "GET",
    data: {action:sendaction}
    }).done(function(data) {
        var pos=data.split(",");
        homescreenwidth=Number(pos[0]);
        homescreenheight=Number(pos[1]);
    });
}

function sendclick(mx,my) {
  var clicktype=$("input[name='mouseclick']:checked").val();
  var mousebutton=$("input[name='mousebutton']:checked").val();
  var sendaction;
  if (mousebutton==="mouseleft") {
    if (clicktype==="singleclick") {
      sendaction="click";
    } else {
      sendaction="clickd";
    }
  } else {
    if (clicktype==="singleclick") {
      sendaction="clickr";
    } else {
      sendaction="clickrd";
    }
  }
  $.ajax({
    url: "/receiveinput",
    method: "GET",
    data: {action:sendaction,x:mx,y:my}
    }).done(getmouse);
}

function sendrclick(mx,my) {
  var sendaction="clickr";
  $.ajax({
    url: "/receiveinput",
    method: "GET",
    data: {action:sendaction,x:mx,y:my}
  }).done(getmouse);
}

function sendmousedown(mx,my) {
  var sendaction="moused";
  $.ajax({
    url: "/receiveinput",
    method: "GET",
    data: {action:sendaction,x:mx,y:my}
  }).done(getmouse);
}

function sendmousemove(mx,my) {
  var sendaction="mousem";
  $.ajax({
    url: "/receiveinput",
    method: "GET",
    data: {action:sendaction,x:mx,y:my}
  }).done(getmouse);
}

function sendmouseup(mx,my) {
  var sendaction="mouseu";
  $.ajax({
    url: "/receiveinput",
    method: "GET",
    data: {action:sendaction,x:mx,y:my}
  }).done(getmouse);
}

function sendmute() {
  $.ajax({
    url: "/receiveinput",
    method: "GET",
    data: {action:"mute"}
  });
}

function sendvoldown() {
  $.ajax({
    url: "/receiveinput",
    method: "GET",
    data: {action:"voldown"}
  });
}

function sendvolup() {
  $.ajax({
    url: "/receiveinput",
    method: "GET",
    data: {action:"volup"}
  });
}

function sendtext(msg) {
  $.ajax({
    url: "/receiveinput",
    method: "GET",
    data: {action:"sendtext",text:msg}
  });
}

function sendbackspace() {
  $.ajax({
    url: "/receiveinput",
    method: "GET",
    data: {action:"sendbackspace"}
  });
}

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

function mouserefresh() {
  setTimeout(mouserefresh,mousegetrate);
  getmouse();
}

function drawmouse() {
  if (displaywidth!==null && homemousex!==null) {
    var prevx=mousedposx;
    var prevy=mousedposy;
    mousedposx=Math.round(homemousex*displaywidth/homescreenwidth);
    mousedposy=Math.round(homemousey*displayheight/homescreenheight);
    if (prevx!==mousedposx || prevy!==mousedposy) {
      drawpointer(prevx,prevy);
    }
    if ($(".drawfps").prop("checked")) {
      drawfps();
    }
  }
}

function redim() {
  maxwidth= window.innerWidth || document.documentElement.clientWidth || document.body.clientWidth;
  maxheight= window.innerHeight || document.documentElement.clientHeight || document.body.clientHeight;
}

function drawfps() {
  var c = $(".overlay")[0];
  var ctx = c.getContext("2d");
  ctx.fillStyle = "#FFFFFF";
  ctx.fillRect(5,5,310,30);
  ctx.font = "30px Arial";
  ctx.fillStyle = "#000000";
  ctx.fillText(fps,5,30);
}

function gotframe() {
  frames[frameptr]=timestamp=new Date().getTime();
  if (gottwenty) {
    fps=20000/(frames[frameptr]-frames[(frameptr+1)%19]);
  }
  if (frameptr<19) {
    frameptr++;
  } else {
    gottwenty=true;
    frameptr=0;
  }
}

function checkifstalled() {
  var newts = new Date().getTime();
  if (newts-timestamp>timeout) {
    timestamp=new Date().getTime();
    redim();
    if ($(".first").css("z-index")==="13") {
      $(".second").attr("src","/screen.jpeg?w="+maxwidth+"&h="+maxheight+"&ts="+timestamp);
    } else {
      $(".first").attr("src","/screen.jpeg?w="+maxwidth+"&h="+maxheight+"&ts="+timestamp);
    }
  }
  setTimeout(checkifstalled,timeout);
}

function calcdistance(x0,y0,x1,y1) {
  return Math.sqrt(Math.pow(Math.abs(x0-x1),2)+Math.pow(Math.abs(y0-y1),2));
}

$(document).ready(function() {
  timestamp=new Date().getTime();
  redim();
  $(".first").attr("src","/screen.jpeg?w="+maxwidth+"&h="+maxheight+"&ts="+timestamp);
  getserverscreensize();
  getmouse();
  setTimeout(checkifstalled,timeout);
  setTimeout(mouserefresh,mousegetrate);

  /*
  var portrait=(window.innerWidth > window.innerHeight? false:true);
  window.onresize = function() {
    var currentorientation=(window.innerWidth > window.innerHeight? false:true);
    if (currentorientation!=portrait) {
      var body=$(".controls").parent()[0];
      var controls=$(".controls")[0];
      var content=$(".content")[0];
      body.removeChild(controls);
      body.removeChild(content);
      if (currentorientation) {
        body.appendChild(content);
        body.appendChild(controls);
      } else {
        body.appendChild(controls);
        body.appendChild(content);
      }
      portrait=currentorientation;
    }
  }
  */
  $(".first").on("load",function() {
    var newts = new Date().getTime();
    if (newts-timestamp<refreshrate)
      newts=refreshrate-newts+timestamp;
    else
      newts=0;
    setTimeout(function() {
      if ($(".drawfps").prop("checked")) gotframe();
      timestamp=new Date().getTime();
      $(".first").css("z-index","13");
      $(".second").css("z-index","12");
      displaywidth = $(".first")[0].clientWidth;
      displayheight = $(".first")[0].clientHeight;
      $(".overlay").css("width",displaywidth);
      $(".overlay").css("height",displayheight);
      $(".cvdiv").css("width",displaywidth);
      $(".cvdiv").css("height",displayheight);
      if ($(".overlay")[0].width !== displaywidth || $(".overlay")[0].height!==displayheight) {
        $(".overlay")[0].width=displaywidth;
        $(".overlay")[0].height=displayheight;
        drawpointer(mousedposx,mousedposy);
      }
      redim();
      $(".second").attr("src","/screen.jpeg?w="+maxwidth+"&h="+maxheight+"&ts="+timestamp);
    },newts);
  });
  $(".second").on("load",function() {
    var newts = new Date().getTime();
    if (newts-timestamp<refreshrate)
      newts=refreshrate-newts+timestamp;
    else
      newts=0;
    setTimeout(function() {
      if ($(".drawfps").prop("checked")) gotframe();
      timestamp=new Date().getTime();
      $(".first").css("z-index","12");
      $(".second").css("z-index","13");
      displaywidth = $(".second")[0].clientWidth;
      displayheight = $(".second")[0].clientHeight;
      $(".overlay").css("width",displaywidth);
      $(".overlay").css("height",displayheight);
      $(".cvdiv").css("width",displaywidth);
      $(".cvdiv").css("height",displayheight);
      if ($(".overlay")[0].width !== displaywidth || $(".overlay")[0].height!==displayheight) {
        $(".overlay")[0].width=displaywidth;
        $(".overlay")[0].height=displayheight;
        drawpointer(mousedposx,mousedposy);
      }
      redim();
      $(".first").attr("src","/screen.jpeg?w="+maxwidth+"&h="+maxheight+"&ts="+timestamp);
    },newts);
  });
  $(".overlay").on("contextmenu", function(e){
    if (e.which===3) {
      var offset = $(this).offset();
      var X = (e.pageX - offset.left);
      var Y = (e.pageY - offset.top);
      var hX=Math.round(homescreenwidth * X / displaywidth);
      var hY=Math.round(homescreenheight * Y / displayheight);
      sendrclick(hX,hY);
    }
    e.preventDefault();
    return false;
  });
  $(".overlay")
  .mousedown(function(e) {
    if (e.which===1) {
      isDragging = false;
      canceldrag = true;
      mdown=true;
      var offset = $(this).offset();
      var X = (e.pageX - offset.left);
      var Y = (e.pageY - offset.top);
      var hX=Math.round(homescreenwidth * X / displaywidth);
      var hY=Math.round(homescreenheight * Y / displayheight);
      startx=hX;
      starty=hY;
      lastx=hX;
      lasty=hY;
      sendmousemove(hX,hY);
    }
  })
  .mousemove(function(e) {
    if (mdown) {
      var offset = $(this).offset();
      var X = (e.pageX - offset.left);
      var Y = (e.pageY - offset.top);
      var hX=Math.round(homescreenwidth * X / displaywidth);
      var hY=Math.round(homescreenheight * Y / displayheight);
      if (!isDragging) {
        isDragging = true;
        sendmousemove(hX,hY);
        sendmousedown(startx,starty);
      }
      if (calcdistance(hX,hY,lastx,lasty)>dragstep) {
        lastx=hX;
        lasty=hY;
        canceldrag=false;
        sendmousemove(hX,hY);
      }
    }
  })
  .mouseup(function(e) {
    if (e.which===1) {
      mdown=false;
      var wasDragging = isDragging;
      isDragging = false;
      var offset = $(this).offset();
      var X = (e.pageX - offset.left);
      var Y = (e.pageY - offset.top);
      var hX=Math.round(homescreenwidth * X / displaywidth);
      var hY=Math.round(homescreenheight * Y / displayheight);
      if (wasDragging && !canceldrag) {
        sendmousemove(hX,hY);
        sendmouseup(hX,hY);
      } else {
        sendclick(hX,hY);
      }
      canceldrag=true;
    }
  })
  .on("touchstart",function(e) {
    isDragging = false;
    var touches = e.changedTouches;
    var offset = $(this).offset();
    var X = (touches[touches.length-1].pageX - offset.left);
    var Y = (touches[touches.length-1].pageY - offset.top);
    var hX=Math.round(homescreenwidth * X / displaywidth);
    var hY=Math.round(homescreenheight * Y / displayheight);
    startx=hX;
    starty=hY;
  })
  .on("touchmove",function(e) {
    var touches = e.changedTouches;
    var offset = $(this).offset();
    var X = (touches[touches.length-1].pageX - offset.left);
    var Y = (touches[touches.length-1].pageY - offset.top);
    var hX=Math.round(homescreenwidth * X / displaywidth);
    var hY=Math.round(homescreenheight * Y / displayheight);
    if (!isDragging) {
      isDragging = true;
      sendmousemove(hX,hY);
      sendmousedown(startx,starty);
    }
    if (calcdistance(hX,hY,lastx,lasty)>dragstep) {
      lastx=hX;
      lasty=hY;
      sendmousemove(hX,hY);
    }
  })
  .on("touchend",function(e) {
    var wasDragging = isDragging;
    isDragging = false;
    var touches = e.changedTouches;
    var offset = $(this).offset();
    var X = (touches[touches.length-1].pageX - offset.left);
    var Y = (touches[touches.length-1].pageY - offset.top);
    var hX=Math.round(homescreenwidth * X / displaywidth);
    var hY=Math.round(homescreenheight * Y / displayheight);
    if (wasDragging) {
      sendmousemove(hX,hY);
      sendmouseup(hX,hY);
    }
  });
  var overcanvas=false;
  $(".overlay").mouseover(function(e) {
    overcanvas=true;
  });
  $(".overlay").mouseout(function(e) {
    overcanvas=false;
  });
  $(document).on("keypress",function(e) {
    if (overcanvas) {
      sendtext(String.fromCharCode(e.which));
      e.preventDefault();
      return false;
    }
  });
  $(".sendtext").click(function() {
    sendtext($(".texttosend").val());
  });
  $(".sendback").click(function() {
    sendbackspace();
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
  $(".drawfps").change(function() {
    if (!$(".drawfps").prop("checked")) {
      var c = $(".overlay")[0];
      var ctx = c.getContext("2d");
      ctx.clearRect(5,5,310,30);
    }
  });
  $(".hidecontrols").click(function() {
    $(".controls").hide();
  });
  $(".closesettings").click(function() {
	$(".settings").hide();
  });
  $(".opensettings").click(function() {
	if ($(".settings").is(":visible"))
	  $(".settings").hide();
	else
	  $(".settings").show();
  });
  $("input[name='Save']").click(function() {
    var settingsform = $("form[name='savesettings']")[0]
    $.ajax({
	  url: "/savesettings",
	  method: "GET",
	  data: {
		  Audio:settingsform.elements["Audio"].checked,
		  Timeout:settingsform.elements["Timeout"].value,
		  Resolution:settingsform.elements["Resolution"].value,
		  LogLevel:settingsform.elements["LogLevel"].value,
		  Port:settingsform.elements["Port"].value
	  }
	});
	$(".settings").hide();
  });
  $("input[name='Restart']").click(function() {
    window.location="/restart";
  });
  $("input[name='Shutdown']").click(function() {
    if (confirm("Are you sure you want to stop the server?")) {
      window.location="/exit";
    }
  });
});