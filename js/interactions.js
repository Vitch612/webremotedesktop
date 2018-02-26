var homemousex;
var homemousey;
function getmouse() {
  $.ajax({
    url: "/windows/getmouse.php",
    method: "GET",
    }).done(function(data) {
        var pos=data.split(",");
        homemousex=pos[0];
        homemousey=pos[1];
    });
}
var homescreenwidth;
var homescreenheight;
function getserverscreensize() {
  $.ajax({
    url: "/windows/getscreenresolution.php",
    method: "GET",
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
  if (mousebutton=="mouseleft") {
    if (clicktype=="singleclick") {
      sendaction="click";
    } else {
      sendaction="clickd";
    }      
  } else {
    if (clicktype=="singleclick") {
      sendaction="clickr";
    } else {
      sendaction="clickrd";  
    }            
  }  
  $.ajax({
    url: "/windows/receiveinput.php",
    method: "POST",
    data: {action:sendaction,x:mx,y:my}
    }).done(getmouse);
}

function sendmute() {
  $.ajax({
    url: "/windows/receiveinput.php",
    method: "POST",
    data: {action:"mute"}
  });
}

function sendvoldown() {
  $.ajax({
    url: "/windows/receiveinput.php",
    method: "POST",
    data: {action:"voldown"}
  });
}

function sendvolup() {
  $.ajax({
    url: "/windows/receiveinput.php",
    method: "POST",
    data: {action:"volup"}
  });
}
