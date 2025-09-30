//code needed for datatable to work in TH index page
var dataTable;

$(document).ready(function () {
    loadDataTable();
});

function loadDataTable() {
    dataTable = $('#tblData').DataTable({
        "ajax": { url: '/User/TransactionHeaders/Index?handler=All' },//this url will call the OnGetAll method in the page model which returns all the THs in json format.
        "dom": '<"d-flex justify-content-between align-items-center mb-2"l<"ml-auto"f>>rtip',
        "columns": [//defining the columns of the datatable and mapping them to the properties of the TH model.
            { data: 'printerModel', "width": "25%" },//dont forget the names should match the property names.
            { data: 'serialNumber', "width": "15%" },
            {
                data: 'status',
                "render": function (data) {
                    switch (data) {
                        case "New":
                            return `<span class="badge bg-info p-2 fs-5">${data}</span>`;
                        case "InProgress":
                            return `<span class="badge bg-warning p-2 fs-5">${data}</span>`;
                        case "Completed":
                            return `<span class="badge bg-success p-2 fs-5">${data}</span>`;
                        case "Cancelled":
                            return `<span class="badge bg-danger p-2 fs-5">${data}</span>`;
                        default:
                            return `<p>${data}</p>`;
                    }
                },
                "width": "10%"
            },
            { data: 'client.name', "width": "15%" },
            { data: 'createdDate', "width": "15%" },
            {
                data: 'id',
                "render": function (data) {//this is to render the parts, edit and delete buttons in the last column.
                    return `<div class="w-75 d-flex" role="group">
                    <a href="/User/TransactionBodies/Index?HeaderId=${data}" title="View Parts" class="btn btn-info mx-2"><i class="bi bi-tools"></i></a> 
                    <a href="/User/TransactionHeaders/Upsert?id=${data}" title="Edit" class="btn btn-primary mx-2"><i class="bi bi-pencil-square"></i></a>
                    <!--onclick is for initiating Delete function and passing the url with id-->
                    <a onClick=Delete('/User/TransactionHeaders/Index?handler=Delete&id=${data}') title="Delete" class="btn btn-danger mx-2"><i class="bi bi-trash-fill"></i></a>
                    <a onClick=Cancel('/User/TransactionHeaders/Index?handler=Cancel&id=${data}') title="Cancel" class="btn btn-warning mx-2"><i class="bi bi-x-circle"></i></a>
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
                    dataTable.ajax.reload();//reload the datatable to reflect the changes.
                    toastr.success(data.message);//show success message using toastr.
                }
            })
        }
    });
}

//function for sweet alert cancel confirmation
function Cancel(url) {
    Swal.fire({
        title: "Are you sure?",
        text: "You won't be able to revert this!",
        icon: "warning",
        showCancelButton: true,
        confirmButtonColor: "#3085d6",
        cancelButtonColor: "#d33",
        confirmButtonText: "Yes, Cancel Transaction!"
    }).then((result) => {
        if (result.isConfirmed) {//if user clicks on yes, cancel it button
            $.ajax({
                url: url,//url is passed from the Cancel function call in the datatable render method.
                success: function (data) {//data is the json returned from the OnGetCancel method in the page model.
                    dataTable.ajax.reload();//reload the datatable to reflect the changes.
                    toastr.success(data.message);//show success message using toastr.
                }
            })
        }
    });
}


