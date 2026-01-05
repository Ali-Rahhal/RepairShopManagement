var dataTable;

function loadCategories() {
    $.get('/Admin/Models/Index?handler=Categories', function (categories) {
        populateCategoryFilter(categories);
    });
}

$(function () {
    loadCategories();   // ✅ load once
    loadDataTable();
});

function isAdmin() {
    return document.getElementById("isAdmin").value === "True";
}

function loadDataTable() {
    dataTable = $('#tblData').DataTable({
        serverSide: true,
        processing: true,
        paging: true,
        pageLength: 10,
        lengthMenu: [5, 10, 25, 50],
        searchDelay: 500, // Add delay for better UX with server-side
        stateSave: true, // Keep user's state
        stateDuration: 86400, // 24 hours in seconds
        order: [[1, "asc"]], // default: Category ascending
        ajax: {
            url: '/Admin/Models/Index?handler=All',
            type: 'GET',
            data: function (d) {
                d.category = $('#categoryFilter').val(); // ✅ send category
            }
        },
        "dom": '<"d-flex justify-content-between align-items-center mb-2"l<"ml-auto"f>>rtip',

        columns: [
            {
                data: 'name',
                width: "35%",
                render: function (data) {
                    return data || '-';
                }
            },
            {
                data: 'category',
                width: "30%",
                render: function (data) {
                    return data || 'Uncategorized';
                }
            },
            {
                data: 'id',
                orderable: false,
                width: "25%",
                render: function (data) {
                    return `
                        <div class="w-100 d-flex justify-content-center">
                            <a href="/Admin/Models/Upsert?id=${data}"
                               class="btn btn-primary mx-2">
                               <i class="bi bi-pencil-square"></i>
                            </a>
                            <a onclick="Delete('/Admin/Models/Index?handler=Delete&id=${data}')"
                               class="btn btn-danger mx-2">
                               <i class="bi bi-trash-fill"></i>
                            </a>
                        </div>`;
                }
            }
        ],

        language: {
            emptyTable: "No models found",
            zeroRecords: "No matching models found"
        }
    });

    dataTable.column(2).visible(isAdmin());

    $('#categoryFilter').on('change', applyCategoryFilter);
}

// ================= CATEGORY FILTER =================

function populateCategoryFilter(categories) {
    var select = $('#categoryFilter');
    select.empty();
    select.append('<option value="All">All Categories</option>');

    categories.sort(function (a, b) {
        return a.localeCompare(b, undefined, { sensitivity: 'base' });
    });

    categories.forEach(function (category) {
        select.append(`<option value="${category}">${category}</option>`);
    });
}

function applyCategoryFilter() {
    dataTable.ajax.reload(); // ✅ server handles filtering
}

function clearCategoryFilter() {
    $('#categoryFilter').val('All');
    dataTable.order([[1, 'asc']]).ajax.reload(); // reset ordering to default
    toastr.info('Category filter and ordering reset');
}

// ================= DELETE =================

function Delete(url) {
    Swal.fire({
        title: "Are you sure you want to delete this model?",
        text: "This action cannot be undone!",
        icon: "warning",
        showCancelButton: true,
        confirmButtonText: "Yes, delete it!"
    }).then((result) => {
        if (result.isConfirmed) {
            $.ajax({
                url: url,
                type: 'GET',
                success: function (data) {
                    if (data.success) {
                        dataTable.ajax.reload(null, false);
                        toastr.success(data.message);
                    } else {
                        toastr.error(data.message);
                    }
                },
                error: function () {
                    toastr.error('An error occurred while deleting the model');
                }
            });
        }
    });
}
