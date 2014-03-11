$(function() {

    // searches for the given term and fills the results
    var search = function(term, callback) {
        $.ajax({
            url: '/path/to/service.xml',
            type: 'POST',
            dataType: 'xml',
            success: function(data, status, xhr) {
                // fill results
                callback(null, data);
            },
            error: function(xhr, status, error) {
                // show error message, empty results
                callback(error, null);
            }
        });
    }

    // looks up the autocomplete strings and shows them
    var autocomplete = function(string, callback) {
        $("#autocomplete")
            .show()
            .addClass("loading");

        $.ajax({
            url: '/path/to/service.xml',
            type: 'POST',
            dataType: 'xml',
            success: function(data, status, xhr) {
                $("#autoloading").removeClass("loading");
                // fill results
                callback(null, data);
            },
            error: function(xhr, status, error) {
                // show error message, empty results
                callback(error, null);
            }
        });
    }

    // whenever the something is typed in the searchbar
    var _t1 = null;
    var _t2 = null;
    $("#search").on('keyup', function(evt) {
        // show loading bar

        $('#results').addClass('loading');

        // get autocomplete
        _t2 && clearTimeout(_t2);
        _t2 = setTimeout(function() {
            _t2 = null;

            // autocomplete
            autocomplete(evt.target.value, function() {
                // do nothing?
            });

        }, 100);

        // 500 ms after typing is finished, autosearch
        _t1 && clearTimeout(_t1);
        _t1 = setTimeout(function() {
            _t1 = null;

            // search
            search(evt.target.value, function() {
                $('#results').removeClass('loading');
            });

        }, 1000);

        // if enter is pressed, immediately search and clear timeout
    }).on('keydown', function(evt) {
        if (evt.which === 13) {
            evt.preventDefault();
            clearTimeout(_t1);
            _t1 = null;

            search(evt.target.value, function() {
                $('#results').removeClass('loading');
            });
        }
    });

    $("#autocomplete").on('click', 'li', function(evt) {
        var e = $.Event('keydown');
        e.which = 13;
        $("#search").val(evt.target.innerHTMl).trigger(e);
    });

    $(document.body).on('click', function() {
        $('#autocomplete').hide();
    })

});