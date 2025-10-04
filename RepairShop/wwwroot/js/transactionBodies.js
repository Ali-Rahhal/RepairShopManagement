//code needed for datatable to work in TH index page
var dataTable;
let hId = document.getElementById("HeaderId").value;
let hStatus = document.getElementById("HeaderStatus").value;

$(document).ready(function () {
    loadDataTable();
});

function isAdmin() {//function to check if the user is admin
    return document.getElementById("isAdmin").value === "True";
}

function loadDataTable() {
    dataTable = $('#tblData').DataTable({
        "ajax": { url: `/User/TransactionBodies/Index?handler=All&headerId=${hId}` },
        "dom": '<"d-flex justify-content-between align-items-center mb-2"l<"ml-auto"f>>rtip',
        "columns": [
            { data: 'brokenPartName', "width": "25%" },//dont forget the names should match the property names.
            {
                data: 'status',
                "render": function (data) {
                    switch (data) {
                        case "PendingForRepair":
                            return `<span class="badge bg-info p-2 fs-5">${data}</span>`;
                        case "PendingForReplacement":
                            return `<span class="badge bg-warning p-2 fs-5">${data}</span>`;
                        case "Fixed":
                            return `<span class="badge bg-success p-2 fs-5">${data}</span>`;
                        case "Replaced":
                            return `<span class="badge bg-success p-2 fs-5">${data}</span>`;
                        case "NotRepairable":
                            return `<span class="badge bg-danger p-2 fs-5">${data}</span>`;
                        case "NotReplaceable":
                            return `<span class="badge bg-danger p-2 fs-5">${data}</span>`;
                        default:
                            return `<p>${data}</p>`;
                    }
                },
                "width": "20%"
            },
            { data: 'part.name', "width": "25%" },//dont forget the names should match the property names.
            {
                data: 'id',
                visible: !isAdmin() && hStatus !== "Completed",//only show this column if the user is not admin status is not completed
                "render": function (data, type, row) {
                    return `<div class="w-100 d-flex justify-content-center" role="group">
                    <a href="/User/TransactionBodies/Upsert?id=${data}" title="Edit Name" class="btn btn-primary mx-2"><i class="bi bi-pencil-square"></i></a>
                    <!--onclick is for initiating Delete function and passing the url with id-->
                    <a onClick="Delete('/User/TransactionBodies/Index?handler=Delete&id=${data}')" title="Delete" class="btn btn-danger mx-2"><i class="bi bi-trash-fill"></i></a>
                    <a onClick="ChangeStatus(${data}, '${row.status}')" title="Change Status" class="btn btn-warning mx-2"><i class="bi bi-gear"></i></a>
                    </div>`
                },
                "width": "30%"
            }
        ]
    });
}


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

function ChangeStatus(id, currentStatus) {
    let confirmButtonText, denyButtonText;
    let confirmUrl, denyUrl;

    if (currentStatus === 'PendingForRepair') {
        confirmButtonText = 'Fixed';
        denyButtonText = 'Not Repairable';
        confirmUrl = `/User/TransactionBodies/Index?handler=Status&id=${id}&choice=1`; // Fixed
        denyUrl = `/User/TransactionBodies/Index?handler=Status&id=${id}&choice=0`; // Not Repairable
    } else if (currentStatus === 'PendingForReplacement') {
        confirmButtonText = 'Replaced';
        denyButtonText = 'Not Replaceable';
        confirmUrl = `/User/TransactionBodies/Index?handler=Status&id=${id}&choice=3`; // Replaced
        denyUrl = `/User/TransactionBodies/Index?handler=Status&id=${id}&choice=2`; // Not Replaceable
    } else {
        // If status is already final, show appropriate message
        toastr.info('This part already has a final status and cannot be changed.');
        return;
    }

    Swal.fire({
        title: 'Choose Action',
        text: 'What is the status of your part?',
        icon: 'question',

        // --- Button Configuration ---
        showConfirmButton: true, // Shows the "Confirm" button
        showDenyButton: true,   // Shows the "Deny" button
        showCancelButton: true, // Shows the "Cancel" button

        // Text for the buttons (dynamic based on status)
        confirmButtonText: confirmButtonText,
        denyButtonText: denyButtonText,
        cancelButtonText: 'Close',

        // Button colors (optional, for better visual distinction)
        confirmButtonColor: '#198754', // Green for success actions
        denyButtonColor: '#dc3545',    // Red for negative actions
        cancelButtonColor: '#6c757d'   // Gray for cancel

    }).then((result) => {
        if (result.isConfirmed) {
            // CONFIRM Button clicked (Fixed or Replaced)
            $.ajax({
                url: confirmUrl,
                success: function (data) {
                    if (data.success) {
                        dataTable.ajax.reload();
                        toastr.success(data.message);
                    } else {
                        toastr.error(data.message);
                    }
                },
                error: function () {
                    toastr.error('An error occurred while updating status.');
                }
            })
        } else if (result.isDenied) {
            // DENY Button clicked (Not Repairable or Not Replaceable)
            $.ajax({
                url: denyUrl,
                success: function (data) {
                    if (data.success) {
                        dataTable.ajax.reload();
                        toastr.success(data.message);
                    } else {
                        toastr.error(data.message);
                    }
                },
                error: function () {
                    toastr.error('An error occurred while updating status.');
                }
            })
        }
        // If result.isDismissed (Cancel or background click), nothing happens.
    });
}