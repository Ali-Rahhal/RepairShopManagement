//code needed for datatable to work in TH index page
var dataTable;

$(document).ready(function () {
    loadDataTable();

    /**
    * This code block sets up event listeners for the apply filters button
    */
    $('#applyFilters').on('click', function () {
        dataTable.draw();
    });

    // Initialize custom multi-select
    initializeCustomMultiSelect();
});

function initializeCustomMultiSelect() {
    const toggle = document.getElementById('customMultiSelectToggle');
    const dropdown = document.getElementById('customMultiSelectDropdown');
    const selectedValuesInput = document.getElementById('selectedValues');

    let selectedValues = [];

    // Toggle dropdown
    toggle.addEventListener('click', function () {
        dropdown.classList.toggle('show');
    });

    // Close dropdown when clicking outside
    document.addEventListener('click', function (event) {
        if (!event.target.closest('.custom-multiselect')) {
            dropdown.classList.remove('show');
        }
    });

    function updateSelectedValues() {
        selectedValuesInput.value = selectedValues.map(v => v.value).join(',');
    }

    function updateToggleText() {
        if (selectedValues.length === 0) {
            toggle.textContent = 'Select clients...';
        } else {
            const selectedText = selectedValues.map(v => v.text).join(', ');
            toggle.textContent = selectedText.length > 30
                ? `${selectedValues.length} client(s) selected`
                : selectedText;
        }
    }

    // Function to populate dropdown options
    window.populateClientDropdown = function (clientNames) {
        dropdown.innerHTML = ''; // Clear existing options

        clientNames.forEach(clientName => {
            const option = document.createElement('div');
            option.className = 'multiselect-option';
            option.setAttribute('data-value', clientName);
            option.textContent = clientName;

            option.addEventListener('click', function () {
                const value = this.getAttribute('data-value');
                const text = this.textContent;

                if (this.classList.contains('selected')) {
                    // Remove selection
                    this.classList.remove('selected');
                    selectedValues = selectedValues.filter(v => v.value !== value);
                } else {
                    // Add selection
                    this.classList.add('selected');
                    selectedValues.push({ value, text });
                }

                updateSelectedValues();
                updateToggleText();
            });

            dropdown.appendChild(option);
        });
    }

    // Function to get selected client values
    window.getSelectedClients = function () {
        return selectedValues.map(v => v.value);
    }
}

function isAdmin() {//function to check if the user is admin
    return document.getElementById("isAdmin").value === "True";
}

function loadDataTable() {
    dataTable = $('#tblData').DataTable({
        "ajax": {
            url: '/User/TransactionHeaders/Index?handler=All',
            dataSrc: function (json) {
                // Populate custom multi-select dropdown dynamically
                var uniqueClients = [...new Set(json.data.map(item => item.client.name))];
                uniqueClients.sort();

                // Populate the custom multi-select dropdown
                if (typeof window.populateClientDropdown === 'function') {
                    window.populateClientDropdown(uniqueClients);
                }

                return json.data;
            }
        },
        "dom": '<"d-flex justify-content-between align-items-center mb-2"l<"ml-auto"f>>rtip',
        "columns": [
            {
                data: 'user.userName',
                visible: isAdmin(),//this column is only visible to admin users
                "width": "10%"
            },
            { data: 'model', "width": "25%" },
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
            {
                data: 'createdDate',
                "width": "15%",
                "render": function (data) {
                    if (data) {
                        // Convert to Date object and format as dd-MM-yyyy HH:mm tt
                        const date = new Date(data);
                        return date.toLocaleDateString('en-GB')
                            .split('/').join('-') + ' ' +
                            date.toLocaleTimeString('en-US', {
                                hour: '2-digit',
                                minute: '2-digit',
                                hour12: true
                            });
                    }
                    return '';
                }
            },
            {
                data: 'id',
                visible: isAdmin(),//this column is only visible to non-admin users
                "render": function (data) {
                    return `<div class="w-75 d-flex" role="group">
                    <a href="/User/TransactionBodies/Index?HeaderId=${data}" title="View Parts" class="btn btn-info mx-2"><i class="bi bi-tools"></i></a>
                    </div>`;
                },
                "width": "5%"
            },
            {
                data: 'id',
                visible: !isAdmin(),//this column is only visible to non-admin users
                "render": function (data) {
                    return `<div class="w-75 d-flex" role="group">
                    <a href="/User/TransactionBodies/Index?HeaderId=${data}" title="View Parts" class="btn btn-info mx-2"><i class="bi bi-tools"></i></a> 
                    <a href="/User/TransactionHeaders/Upsert?id=${data}" title="Edit" class="btn btn-primary mx-2"><i class="bi bi-pencil-square"></i></a>
                    <a onClick=Delete('/User/TransactionHeaders/Index?handler=Delete&id=${data}') title="Delete" class="btn btn-danger mx-2"><i class="bi bi-trash-fill"></i></a>
                    <a onClick=Cancel('/User/TransactionHeaders/Index?handler=Cancel&id=${data}') title="Cancel" class="btn btn-warning mx-2"><i class="bi bi-x-circle"></i></a>
                    </div>`;
                },
                "width": "20%"
            }
        ]
    });

    // Custom filtering function for date + custom multi-select client
    $.fn.dataTable.ext.search.push(
        function (settings, data, dataIndex) {
            var min = $('#minDate').val();
            var max = $('#maxDate').val();

            // Get selected clients from custom multi-select
            var selectedClients = typeof window.getSelectedClients === 'function'
                ? window.getSelectedClients()
                : [];

            var rowData = dataTable.row(dataIndex).data();
            var createdDate = new Date(rowData.createdDate);
            var clientName = rowData.client.name;

            // Date filter
            if (min) {
                var minDate = new Date(min);
                minDate.setHours(0, 0, 0, 0); // Start of day
                if (createdDate < minDate) return false;
            }
            if (max) {
                var maxDate = new Date(max);
                maxDate.setHours(23, 59, 59, 999); // End of day
                if (createdDate > maxDate) return false;
            }

            // Custom multi-select client filter
            if (selectedClients && selectedClients.length > 0) {
                // Check if client name is in selected clients
                if (!selectedClients.includes(clientName)) return false;
            }

            return true;
        }
    );
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


