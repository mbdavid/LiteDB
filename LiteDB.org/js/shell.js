var ver = 3;
var db = localStorage.getItem('db');

if (!db || db.substr(0, 3) != 'db' + ver) {
    db = 'db' + ver + '_' + (new Date()).getTime();
    localStorage.setItem('db', db);
}

var hist = [];
var idx = 0;

$('.shell').on('click', function () { $('.textbox').focus(); });

$('.textbox').on('keyup', function (e) {

    var $el = $(this);
    var $prompt = $el.parent();
    var text = $el.val().trim();

    if (e.keyCode == 13 && text.length > 0) {

        $prompt.before('<div class="prompt">' + text + '</div>');
        $el.val('')
        $prompt.hide();
        hist.push(text);
        idx = hist.length;

        //$.post('api.ashx', { db: db, cmd: text }).done(function (result) {
        var result = "this tis a messagem from server"
            var css = /^ERROR:\s/.test(result) ? 'error' : 'result';
            $prompt.before('<pre class="' + css + '">' + $('<div/>').text(result).html() + '</pre>');
            $prompt.show();
            $el.focus();
            setTimeout(function () {
                var div = $('.shell').get(0);
                div.scrollTop = div.scrollHeight;
            }, 100);
        //});
    }
    else if (e.keyCode == 38) {
        idx = Math.max(0, idx - 1);
        $el.val(hist[idx]);
    }
    else if (e.keyCode == 40) {
        idx = Math.min(hist.length - 1, idx + 1);
        $el.val(hist[idx]);
    }

});
