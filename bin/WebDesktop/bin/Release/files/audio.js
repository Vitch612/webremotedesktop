var playaudio=true;

function setmsg(text) {
  $(".message").html(text);
}

function addmsg(text) {
  $(".message").html($(".message").html()+" "+text);
}

function startplay() {
  playaudio=true;
  $("#aplay")[0].play();
}

function stopplay() {
  playaudio=false;
  $("#aplay")[0].pause();
}

function currentbufferend() {
  $.ajax({
    url: "/resetposition",
    method: "GET",
    data: {}
  }).done(function(data) {
    $("#aplay")[0].src="/audio.mp3?"+data;
    $("#aplay")[0].load();
  });
}

function tryload() {
  if (!playaudio || $(".stopaudio").prop("checked") || !$("#aplay")[0].paused) {
    return;
  }
  $("#aplay")[0].load();
  addmsg("tryload("+$("#aplay")[0].readyState+")");
  setTimeout(tryload,500);
}

function checkifplaying() {
  if ($("#aplay")[0].paused)
    $("#aplay")[0].play();
  else
    tryload();
}

function tryplay() {
  if (!playaudio || $(".stopaudio").prop("checked")) {
    return;
  }
  addmsg("tryplay("+$("#aplay")[0].readyState+")");
  $("#aplay")[0].play();
  setTimeout(checkifplaying,10);
}

function handleaudio() {
  var aud = $("#aplay")[0];
  addmsg("source("+aud.currentSrc+")");
  tryload();
  aud.volume=1;
  aud.controls = true;
  aud.oncanplay = function() {
    tryplay();
  };
  aud.onabort = function() {
    addmsg("abort("+$("#aplay")[0].readyState+")");
  };
  aud.onerror = function() {
    addmsg("error("+$("#aplay")[0].readyState+")="+$("#aplay")[0].error.code);
    currentbufferend();
  };
  aud.onsuspend = function() {
    addmsg("suspend("+$("#aplay")[0].readyState+")");
  };
  aud.onloadeddata  = function() {
    addmsg("loadeddata("+$("#aplay")[0].readyState+")");;
  };
  aud.onstalled = function() {
    addmsg("stalled("+$("#aplay")[0].readyState+")");
  };
  aud.onended  = function() {
    addmsg("ended("+$("#aplay")[0].readyState+")");
    currentbufferend();
  };
  aud.onloadstart = function() {
    addmsg("loadstart("+$("#aplay")[0].readyState+")");
  };
  aud.onwaiting = function() {
    addmsg("waiting("+$("#aplay")[0].readyState+")");
  };
  aud.onprogress= function() {
    //addmsg("progress("+$("#aplay")[0].readyState+")");
  };
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