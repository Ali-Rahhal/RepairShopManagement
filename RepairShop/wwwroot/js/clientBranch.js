//code needed for datatable to work in client index page
var dataTable;
let clientId = document.getElementById("parentId").value;

$(document).ready(function () {
    loadDataTable();
});

function loadDataTable() {
    dataTable = $('#tblData').DataTable({
        "stateSave": true,
        "ajax": { url: `/User/Clients/ClientBranchIndex?handler=All&ParentId=${clientId}` },
        "dom": '<"d-flex justify-content-between align-items-center mb-2"l<"ml-auto"f>>rtip',
        "columns": [
            { data: 'branchName', "width": "20%" },
            { data: 'phone', "width": "20%" },
            { data: 'email', "width": "20%" },
            { data: 'address', "width": "20%" },
            {
                data: 'id',
                "width": "20%",
                "render": function (data, type, row) {
                    // Add History button
                    return `<div class="w-100 d-flex justify-content-center" role="group">
                        <a href="/User/Clients/ClientSNIndex?id=${data}&parentId=${clientId}" title="View Serial Numbers" class="btn btn-info mx-2"><i class="bi bi-list"></i></a>
                        <a href="/User/Clients/ClientBranchUpsert?id=${data}" title="Edit" class="btn btn-primary mx-2"><i class="bi bi-pencil-square"></i></a>
                        <a onClick="Delete('/User/Clients/Index?handler=Delete&id=${data}')" title="Delete" class="btn btn-danger mx-2"><i class="bi bi-trash-fill"></i></a>
                    </div>`;
                },
                "orderable": false,
                "searchable": false
            }
        ]
    });
}

function Delete(url) {
    Swal.fire({
        title: "Are you sure you want to delete this branch?",
        html: "<p style='color: #dc3545; font-weight: 500;'>This will delete the maintenace contracts associated with this branch if there are any!</p>",
        icon: "warning",
        showCancelButton: true,
        confirmButtonColor: "#3085d6",
        cancelButtonColor: "#d33",
        confirmButtonText: "Yes, delete it!"
    }).then((result) => {
        if (result.isConfirmed) {//if user clicks on yes, delete it button
            $.ajax({
                url: url,//url is passed from the Delete function call in the datatable render method.
                success: function (data) {//data is the json returned from the OnGetDelete method in the page model.
                    if (data.success) {
                        dataTable.ajax.reload();//reload the datatable to reflect the changes.
                        toastr.success(data.message);//show success message using toastr.
                    }
                    else {
                        toastr.error(data.message);//show error message using toastr.
                    }
                }
            })
        }
    });
}