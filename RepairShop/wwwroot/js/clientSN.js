//code needed for datatable to work in client index page
var dataTable;
let clientId = document.getElementById("clientId").value;

$(document).ready(function () {
    loadDataTable();
});

function loadDataTable() {
    dataTable = $('#tblData').DataTable({
        serverSide: true,
        processing: true,
        stateSave: true,
        order: [], // Default ordering
        ajax: {
            url: `/User/Clients/ClientSNIndex?handler=All`,
            type: 'GET',
            data: function (d) {
                d.ClientId = clientId; // Pass client ID as parameter
            },
            error: function () {
                toastr.error('Failed to load serial numbers');
            }
        },
        "dom": '<"d-flex justify-content-between align-items-center mb-2"l<"ml-auto"f>>rtip',
        "columns": [
            {
                data: 'value',
                "width": "35%",
                "render": function (data, type, row) {
                    return `<span class="font-monospace">${data}</span>`;
                }
            },
            {
                data: 'modelName',
                "width": "35%",
                "render": function (data) {
                    return `<span class="badge bg-primary">${data}</span>`;
                }
            },
            {
                data: 'contractNumber',
                "width": "20%",
                "render": function (data) {
                    if (data === "No Contract") {
                        return `<span class="badge bg-warning">${data}</span>`;
                    } else {
                        return `<span class="badge bg-success">${data}</span>`;
                    }
                }
            },
            {
                "data": null,
                "width": "10%",
                "render": function (data, type, row) {
                    // Add History button
                    return `<div class="text-center">
                                <a href="/Admin/History/SerialNumberHistory?searchTerm=${encodeURIComponent(row.value)}&returnUrl=${encodeURIComponent(window.location.href)}" 
                                   class="btn btn-outline-info mx-2" 
                                   title="View History">
                                   <i class="bi bi-clock-history"></i>
                                </a>
                            </div>`;
                },
                "orderable": false,
                "searchable": false
            }
        ],
        "language": {
            "emptyTable": "No serial numbers found",
            "zeroRecords": "No matching serial numbers found"
        }
    });
}

function resetSorting() {
    // Reset to default ordering
    dataTable.order([]).draw();

    if (typeof toastr !== 'undefined') {
        toastr.info('Sorting reset to default order');
    }
}