$(function () {

    new TomSelect('#partSelect', {
        valueField: 'id',
        labelField: 'text',
        searchField: 'text',
        preload: true,
        load: function (query, callback) {
            $.get('/Admin/PartReports/Index?handler=Parts', callback);
        }
    });

    $('#reportForm').on('submit', function (e) {
        e.preventDefault();

        $.post(
            '/Admin/PartReports/Index?handler=GenerateReport',
            $(this).serialize(),
            function (html) {
                $('#reportResult').html(html);
            }
        );
    });

});
