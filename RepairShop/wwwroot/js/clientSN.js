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
            { data: 'value', "width": "40%" },
            { data: 'modelName', "width": "40%" },
            { data: 'contractNumber', "width": "20%" }
        ]
    });
}