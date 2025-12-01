//code needed for datatable to work in client index page
var dataTable;
let clientId = document.getElementById("clientId").value;

$(document).ready(function () {
    loadDataTable();
});

function loadDataTable() {
    dataTable = $('#tblData').DataTable({
        "stateSave": true,
        "ajax": { url: `/User/Clients/ClientSNIndex?handler=All&ClientId=${clientId}` },
        "dom": '<"d-flex justify-content-between align-items-center mb-2"l<"ml-auto"f>>rtip',
        "columns": [
            { data: 'value', "width": "35%" },
            { data: 'modelName', "width": "35%" },
            { data: 'contractNumber', "width": "20%" },
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
        ]
    });
}