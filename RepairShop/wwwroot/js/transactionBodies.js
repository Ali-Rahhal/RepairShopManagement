//code needed for datatable to work in TH index page
var dataTable;
let hId = document.getElementById("HeaderId").value;
$(document).ready(function () {
    loadDataTable();
});

function loadDataTable() {
    dataTable = $('#tblData').DataTable({
        "ajax": { url: `/User/TransactionBodies/Index?handler=All&headerId=${hId}` },//this url will call the OnGetAll method in the page model which returns all the TBs of the selected TH in json format.
        "columns": [//defining the columns of the datatable and mapping them to the properties of the TB model.
            { data: 'partName', "width": "30%" },//dont forget the names should match the property names.
            {
                data: 'status',
                "render": function (data) {
                    switch (data) {
                        case "Pending":
                            return `<span class="badge bg-info p-2 fs-5">${data}</span>`;
                        case "Fixed":
                            return `<span class="badge bg-success p-2 fs-5">${data}</span>`;
                        case "NotRepairable":
                            return `<span class="badge bg-danger p-2 fs-5">${data}</span>`;
                        default:
                            return `<p>${data}</p>`;
                    }
                },
                "width": "20%"
            },
            {
                data: 'id',
                "render": function (data) {//this is to render the edit and delete buttons in the last column.
                    return `<div class="w-75 btn-group" role="group">
                    <a href="/User/TransactionBodies/Upsert?id=${data}" class="btn btn-primary mx-2"><i class="bi bi-pencil-square"></i> Edit</a>
                    <!--onclick is for initiating Delete function and passing the url with id-->
                    <a onClick=Delete('/User/TransactionBodies/Index?handler=Delete&id=${data}') class="btn btn-danger mx-2"><i class="bi bi-trash-fill"></i> Delete</a>
                    <a onClick=ChangeStatus(${data}) class="btn btn-warning mx-2"><i class="bi bi-gear"></i> Status</a>
                    </div>`//using `` for multi-line string and ${} for variable interpolation.
                },//anchors only work with Get requests.
                "width": "50%"
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

function ChangeStatus(id) {

    const fixedUrl = `/User/TransactionBodies/Index?handler=Status&id=${id}&choice=1`;
    const notRepairableUrl = `/User/TransactionBodies/Index?handler=Status&id=${id}&choice=0`;

    Swal.fire({
        title: 'Choose Action',
        text: 'What is the status of your part?',
        icon: 'question',

        // --- Button Configuration ---
        showConfirmButton: true, // Shows the "Confirm" button (used here for the 1st URL)
        showDenyButton: true,   // Shows the "Deny" button (used here for the 2nd URL)
        showCancelButton: true, // Shows the "Cancel" button
        
        // Text for the buttons
        confirmButtonText: 'Fixed',
        denyButtonText: 'NotRepairable',
        cancelButtonText: 'Close',

        // Button colors (optional, for better visual distinction)
        confirmButtonColor: '#0d6efd',
        denyButtonColor: '#F54522', 
        cancelButtonColor: '#6c757d'

    }).then((result) => {
        if (result.isConfirmed) {
            // CONFIRM Button clicked (fixed)
            $.ajax({
                url: fixedUrl,
                success: function (data) {//data is the json returned from the OnGetStatus method in the page model.
                    dataTable.ajax.reload();//reload the datatable to reflect the changes.
                    toastr.success(data.message);//show success message using toastr.
                }
            })
        } else if (result.isDenied) {
            // DENY Button clicked (not repairable)
            $.ajax({
                url: notRepairableUrl,
                success: function (data) {//data is the json returned from the OnGetDelete method in the page model.
                    dataTable.ajax.reload();//reload the datatable to reflect the changes.
                    toastr.success(data.message);//show success message using toastr.
                }
            })
        }
        // If result.isDismissed (Cancel or background click), nothing happens.
    });
}


