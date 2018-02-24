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
  $.ajax({
    url: "/windows/receiveinput.php",
    method: "POST",
    data: {action:"click",x:mx,y:my}
    }).done(getmouse);
}
