$(function () {

    $.get("/Admin/ReceptionNotes/Index?handler=ReceptionNotesAlert", function (result) {

        if (!result.hasPending)
            return;

        const dot = `<span class="notification-dot"></span>`;

        $("#adminDropdown").append(dot);
        $("#receptionNotesLink").append(dot);

        // apply tooltip to the whole link
        $("#receptionNotesLink")
            .attr("data-bs-toggle", "tooltip")
            .attr("data-bs-placement", "right")
            .attr("title", result.message);

        new bootstrap.Tooltip($("#receptionNotesLink")[0]);
    });

});