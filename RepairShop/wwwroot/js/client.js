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
        "ajax": { url: '/User/Clients/Index?handler=All' },//this url will call the OnGetAll method in the page model which returns all the clients in json format.
        "dom": '<"d-flex justify-content-between align-items-center mb-2"l<"ml-auto"f>>rtip',
        "columns": [//defining the columns of the datatable and mapping them to the properties of the client model.
            { data: 'name', "width": "20%" },//dont forget the names should match the property names.
            { data: 'phone', "width": "20%" },
            { data: 'email', "width": "20%" },
            { data: 'address', "width": "20%" },
            {
                data: 'id',
                visible: isAdmin(),//only show this column if the user is admin.
                "render": function (data) {//this is to render the edit and delete buttons in the last column.
                    return `<div class="w-100 d-flex justify-content-center" role="group">
                    <a href="/User/Clients/Upsert?id=${data}" title="Edit" class="btn btn-primary mx-2"><i class="bi bi-pencil-square"></i></a>
                    <!--onclick is for initiating Delete function and passing the url with id-->
                    <a onClick=Delete('/User/Clients/Index?handler=Delete&id=${data}') title="Delete" class="btn btn-danger mx-2"><i class="bi bi-trash-fill"></i></a>
                    </div>`//using `` for multi-line string and ${} for variable interpolation.
                },//anchors only work with Get requests.
                "width": "20%"
            }
        ]
    });
}

//function for sweet alert delete confirmation
function Delete(url) {
    Swal.fire({
        title: "Are you sure?",
        text: "You won't be able to revert this!",
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