window.onbeforeunload = function () {
	var reloadScript = document.createElement("script");
	reloadScript.src = "[UNDERWOLF-FILESERVER]reload";
	document.body.appendChild(reloadScript);
};