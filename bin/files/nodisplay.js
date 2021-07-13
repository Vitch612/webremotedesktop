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

$(document).ready(function() {
  $(".btnmute").click(function() {
    sendmute();
  });
  $(".btnvoldown").click(function() {
    sendvoldown();
  });
  $(".btnvolup").click(function() {
    sendvolup();
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
		  Microphone:settingsform.elements["Microphone"].checked,
		  Timeout:settingsform.elements["Timeout"].value,
		  Resolution:settingsform.elements["Resolution"].value,
		  LogLevel:settingsform.elements["LogLevel"].value,
		  Port:settingsform.elements["Port"].value,
		  Buffering:settingsform.elements["Buffering"].value
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