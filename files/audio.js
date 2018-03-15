var playaudio=true;


function startplay() {
  playaudio=true;
  $("#aplay")[0].load();
  setTimeout(tryagain,500);
}

function stopplay() {
  playaudio=false;
  $("#aplay")[0].pause();
}

function tryagain() {
  if (!playaudio || $(".stopaudio").prop("checked")) {
    return;
  }
  var aud = $("#aplay")[0];
  if (aud.duration > 0 && !aud.paused)
      return;
  else
    aud.load();
  setTimeout(tryagain,500);
}

function playifnotplaying() {
  if (!playaudio || $(".stopaudio").prop("checked")) {
    return;
  }
  aud=$("#aplay")[0];
  if (!(aud.duration > 0 && !aud.paused))
    aud.play();
}

function handleaudio() {
  var aud = $("#aplay")[0];
  aud.load();
  aud.volume=1;
  aud.onloadeddata  = function() {playifnotplaying();};
  aud.onstalled = function() {alert("stalled");};
  aud.onerror = function() {aud.load();};
  //aud.onsuspend = function() {};
  aud.onended  = function() {aud.load();};
  aud.oncanplay = function() {playifnotplaying();};
  //aud.onabort = function() {};
  //aud.onwaiting = function() {};
  setTimeout(tryagain,500);
}

$(document).ready(function() {
  $(".stopaudio").change(function() {
    if ($(".stopaudio").prop("checked")) {
      stopplay();
    }
    else {
      startplay();
    }
  });
  handleaudio();
});