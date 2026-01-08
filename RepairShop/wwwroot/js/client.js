//code needed for datatable to work in client index page
var dataTable;

$(document).ready(function () {
    loadDataTable();
});

function isAdmin() {//function to check if the user is admin
    return document.getElementById("isAdmin").value === "True";
}

function loadDataTable() {
    dataTable = $('#tblData').DataTable({
        serverSide: true,            // Enable server-side processing
        processing: true,            // Show processing indicator
        stateSave: true,
        stateDuration: 86400,
        order: [], // Default ordering
        ajax: {
            url: '/User/Clients/Index?handler=All',
            type: 'GET',
            data: function (d) {
                // Add any additional parameters here if needed
                // d.customParam = $('#filterInput').val();
            },
            error: function () {
                toastr.error('Failed to load clients');
            }
        },
        "dom": '<"d-flex justify-content-between align-items-center mb-2"l<"ml-auto"f>>rtip',
        "columns": [
            { data: 'name', "width": "14%" },
            {
                data: 'branchCount',
                "width": "14%",
                "render": function (data) {
                    return `<span class="badge bg-info">${data}</span>`;
                }
            },
            { data: 'phone', "width": "20%" },
            { data: 'email', "width": "20%" },
            { data: 'address', "width": "20%" },
            {
                data: 'id',
                visible: isAdmin(),//only show this column if the user is admin.
                "render": function (data) {//this is to render the edit and delete buttons in the last column.
                    return `<div class="w-100 d-flex justify-content-center" role="group">
                    <a href="/User/Clients/ClientSNIndex?id=${data}" title="View Serial Numbers" class="btn btn-info mx-2"><i class="bi bi-list"></i></a>
                    <a href="/Admin/SerialNumbers/Upsert?clientId=${data}&returnUrl=${encodeURIComponent(window.location.href)}" title="Add Serial Number" class="btn btn-outline-info mx-2"><i class="bi bi-plus-square"></i></a>
                    <a href="/User/Clients/Upsert?id=${data}" title="Edit" class="btn btn-primary mx-2"><i class="bi bi-pencil-square"></i></a>
                    <a href="/User/Clients/ClientBranchIndex?id=${data}" title="View Branches" class="btn btn-warning mx-2"><i class="bi bi-people"></i></a>
                    <a onClick="Delete('/User/Clients/Index?handler=Delete&id=${data}')" title="Delete" class="btn btn-danger mx-2"><i class="bi bi-trash-fill"></i></a>
                    </div>`
                },
                "width": "12%",
                "orderable": false,
                "searchable": false
            }
        ],
        "language": {
            "emptyTable": "No clients found",
            "zeroRecords": "No matching clients found"
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
function Delete(url) {
    Swal.fire({
        title: "Are you sure you want to delete this client?",
        html: "<p style='color: #dc3545; font-weight: 500;'>This will delete the maintenace contracts associated with this client and all its branches if there are any!</p>",
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