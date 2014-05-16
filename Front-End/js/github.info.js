$(function () {
    if (document.location.host.indexOf('.dev') > -1) return;

    var repo = "archit120/Monero-pool";

    $.ajax({
        url: 'https://api.github.com/repos/' + repo,
        dataType: 'jsonp',

        error: function (result) {

        },
        success: function (results) {
            var repo = results.data;
            var watchers = repo.watchers;
            var forks = repo.forks;

            $(".github-watchers").html(watchers);
            $(".github-forks").html(forks);
        }
    })
});