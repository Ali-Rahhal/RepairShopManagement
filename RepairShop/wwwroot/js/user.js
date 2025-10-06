//code needed for datatable to work in user index page
var dataTable;

$(document).ready(function () {
    loadDataTable();
});

function loadDataTable() {
    dataTable = $('#tblData').DataTable({
        "stateSave": true,
        "ajax": { url: '/Admin/Users/Index?handler=All' },//this url will call the OnGetAll method in the page model which returns all the users in json format.
        "dom": '<"d-flex justify-content-between align-items-center mb-2"l<"ml-auto"f>>rtip',
        "columns": [//defining the columns of the datatable and mapping them to the properties of the user model.
            { data: 'userName', "width": "40%" },//dont forget the names should match the property names.
            { data: 'role', "width": "40%" },
            {
                data: 'id',
                "render": function (data) {//this is to render the edit and delete buttons in the last column.
                    return `<div class="w-100 d-flex justify-content-center" role="group">
                    <a href="/Admin/Users/Edit?id=${data}" title="Edit" class="btn btn-primary mx-2"><i class="bi bi-pencil-square"></i></a>
                    <!--onclick is for initiating Delete function and passing the url with id-->
                    <a onClick="Delete('/Admin/Users/Index?handler=Delete&id=${data}')" title="Delete" class="btn btn-danger mx-2"><i class="bi bi-trash-fill"></i></a>
                    </div>`//using `` for multi-line string and ${} for variable interpolation.
                },//anchors only work with Get requests.
                "width": "20%"
            }
        ]
    });
}


function Delete(url) {
    Swal.fire({
        title: "Are you sure you want to delete this user?",
        text: "You won't be able to revert this!",
        icon: "warning",
        showCancelButton: true,
        confirmButtonColor: "#3085d6",
        cancelButtonColor: "#d33",
        confirmButtonText: "Yes, delete it!"
    }).then((result) => {
        if (result.isConfirmed) {
            $.ajax({
                url: url,
                success: function (data) {
                    if (data.success) {
                        dataTable.ajax.reload();
                        toastr.success(data.message);
                    }
                    else {
                        toastr.error(data.message);
                    }
                }
            })
        }
    });
}