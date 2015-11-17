(function() {

    var db = localStorage.getItem('db-v2');

    if (!db) {
        db = 'db_' + (new Date()).getTime();
        localStorage.setItem('db-v2', db);
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

            execute({ db: db, cmd: text }, function (result) {
                var css = /^ERROR:\s/.test(result) ? 'error' : 'result';
                $prompt.before('<pre class="' + css + '">' + $('<div/>').text(result).html() + '</pre>');
                $prompt.show();
                $el.focus();
                setTimeout(function () {
                    var div = $('.shell').get(0);
                    div.scrollTop = div.scrollHeight;
                }, 100);
            });
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

    function execute(params, cb) {
        if (params.cmd == 'help') return cb(help);
        var host = location.hostname == "localhost" ? "" : "http://litedb.azurewebsites.net/";

        $.post(host + 'WebShell.ashx', params).done(function (result) {
            cb(result);
        });
    }

    var help =
'Web Shell Commands - try offline version for more commands\n' +
'==========================================================\n' +
'\n' +
'> show collections\n' +
'    List all collection in database\n' +
'> db.<collection>.insert <jsonDoc>\n' +
'    Insert a new document into collection\n' +
'> db.<collection>.update <jsonDoc>\n' +
'    Update a document inside collection\n' +
'> db.<collection>.delete <filter>\n' +
'    Delete documents using a filter clausule (see find)\n' +
'> db.<collection>.find [top N] <filter>\n' +
'    Show filtered documents based on index search\n' +
'> db.<collection>.count <filter>\n' +
'    Show count rows according query filter\n' +
'> db.<collection>.ensureIndex <field> [unique]\n' +
'    Create a new index document field\n' +
'<filter> = <field> [=|>|>=|<|<=|!=|like|between] <jsonValue>\n' +
'    Filter query syntax\n' +
'<filter> = (<filter> [and|or] <filter> [and|or] ...)\n' +
'    Multi queries syntax\n' +
'\n' +
'Try:\n' +
' > db.customers.insert { _id:1, name:\"John Doe\", age: 37 }\n' +
' > db.customers.ensureIndex name\n' +
' > db.customers.find name like \"John\"\n' +
' > db.customers.find limit 10 (name like \"John\" and _id between [0, 100])\n';

})();

