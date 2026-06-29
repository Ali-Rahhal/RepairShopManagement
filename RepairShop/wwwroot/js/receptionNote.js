let dataTable;

$(document).ready(function () {
    loadDataTable();
});

function loadDataTable() {

    dataTable = $('#tblData').DataTable({
        serverSide: true,
        processing: true,
        paging: true,
        pageLength: 10,
        lengthMenu: [5, 10, 25, 50, 100],
        searchDelay: 500,
        stateSave: true,
        stateDuration: 86400,
        order: [], // default
        ajax: {
            url: '/Admin/ReceptionNotes/Index?handler=All',
            type: 'POST',
            headers: {
                'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val()
            },
            error: function (xhr, error, thrown) {
                console.error('DataTable Ajax Error:', error, thrown);
                toastr.error('Failed to load reception notes. Please try again.');
            }
        },
        "dom": '<"d-flex justify-content-between align-items-center mb-2"l<"ml-auto"f>>rtip',
        columns: [
            {
                data: "code",
                width: "20%"
            },
            {
                data: "clientName",
                width: "30%"
            },
            {
                data: "deviceCount",
                width: "10%"
            },
            {
                data: "createdDate",
                width: "20%",
                render: function (data) {
                    const d = new Date(data);
                    return d.toLocaleDateString("en-GB") +
                        " " +
                        d.toLocaleTimeString("en-US",
                            {
                                hour: "2-digit",
                                minute: "2-digit"
                            });
                }
            },
            {
                data: "isPrinted",
                width: "10%",
                render: function (data) {
                    return data ? '<span class="badge bg-success">Yes</span>' : '<span class="badge bg-warning">No</span>';
                }
            },
            {
                data: "id",
                width: "15%",
                orderable: false,
                render: function (data) {
                    return `
                        <div class="text-center">
                        <button class="btn btn-info btn-sm me-1" title="View Devices" onclick="showItems(${data})">
                            <i class="bi bi-list"></i>
                        </button>
                        <button class="btn btn-primary btn-sm" title="Print Reception Note" onclick="printReceptionNote(${data})">
                            <i class="bi bi-printer"></i>
                        </button>
                        </div>
                    `;
                }
            }
        ]
    });
}

function reloadTable() {

    // Clear column search
    dataTable.columns().search('').draw();

    dataTable.order([]).ajax.reload(null, false);

    toastr.info('Sorting reset');
}

function showItems(id) {
    $.get(`/Admin/ReceptionNotes/Index?handler=Details&id=${id}`,
        function (items) {
            let html = "";
            items.forEach(i => {
                html += `
                    <tr>
                    <td>${i.number}</td>
                    <td>${i.serialNumber}</td>
                    <td>${i.model}</td>
                    </tr>
                `;
            });
            $("#serialNumbersBody").html(html);
            const modal =
                new bootstrap.Modal(document.getElementById("detailsModal"));
            modal.show();
        });
}

function printReceptionNote(id) {
    // Open in new tab
    window.open(`/Admin/ReceptionNotes/Index?handler=Print&id=${id}`, '_blank');
    // Reload the table after a moment
    setTimeout(() => {
        if (dataTable) {
            dataTable.ajax.reload();
        }
    }, 1000);
    toastr.info('Generating reception note...');
}