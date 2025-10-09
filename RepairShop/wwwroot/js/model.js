var dataTable;

$(function () {
    loadDataTable();
});

function loadDataTable() {
    dataTable = $('#tblData').DataTable({
        stateSave: true,
        ajax: {
            url: '/Admin/Models/Index?handler=All',
            dataSrc: function (json) {
                // Extract unique categories for the filter
                var categories = [...new Set(json.data.map(item => item.category || 'Uncategorized'))];
                populateCategoryFilter(categories);
                return json.data;
            }
        },
        dom: '<"d-flex justify-content-between align-items-center mb-2"l<"ml-auto"f>>rtip',
        columns: [
            {
                data: 'name',
                width: "35%",
                render: function (data, type, row) {
                    return data || '-';
                }
            },
            {
                data: 'category',
                width: "30%",
                render: function (data, type, row) {
                    return data || 'Uncategorized';
                }
            },
            {
                data: 'id',
                render: function (data) {
                    return `<div class="w-100 d-flex justify-content-center" role="group">
                        <a href="/Admin/Models/Upsert?id=${data}" title="Edit" class="btn btn-primary mx-2"><i class="bi bi-pencil-square"></i></a>
                        <a onClick="Delete('/Admin/Models/Index?handler=Delete&id=${data}')" title="Delete" class="btn btn-danger mx-2"><i class="bi bi-trash-fill"></i></a>
                    </div>`;
                },
                width: "25%",
                orderable: false
            }
        ],
        language: {
            emptyTable: "No models found",
            zeroRecords: "No matching models found"
        }
    });

    // Add event listener for category filter
    $('#categoryFilter').on('change', function () {
        applyCategoryFilter();
    });

    // Add event listener for DataTable search to sync with category filter
    dataTable.on('search.dt', function () {
        // If there's a text search, clear the category filter to avoid conflicts
        if (dataTable.search().length > 0) {
            $('#categoryFilter').val('All');
        }
    });
}

function populateCategoryFilter(categories) {
    var select = $('#categoryFilter');

    // CLEAR existing options first
    select.empty(); // ← THIS LINE FIXES THE DUPLICATION
    // Sort categories alphabetically, case insensitive
    select.append('<option value="All">All Categories</option>');
    categories.sort(function (a, b) {
        return a.localeCompare(b, undefined, { sensitivity: 'base' });
    });

    categories.forEach(function (category) {
        var displayName = category || 'Uncategorized';
        select.append('<option value="' + category + '">' + displayName + '</option>');
    });
}

function applyCategoryFilter() {
    var category = $('#categoryFilter').val();

    if (category === 'All') {
        // Clear category filter but preserve any text search
        dataTable.column(1).search('').draw();
    } else {
        // Apply exact match for category
        var searchValue = category === 'Uncategorized' ? '^$' : '^' + category + '$';
        dataTable.column(1).search(searchValue, true, false).draw();
    }
}

function clearCategoryFilter() {
    $('#categoryFilter').val('All');
    dataTable.column(1).search('').draw();

    // Show success message
    toastr.info('Category filter cleared');
}

function Delete(url) {
    Swal.fire({
        title: "Are you sure you want to delete this?",
        text: "This action cannot be undone!",
        icon: "warning",
        showCancelButton: true,
        confirmButtonColor: "#3085d6",
        cancelButtonColor: "#d33",
        confirmButtonText: "Yes, delete it!"
    }).then((result) => {
        if (result.isConfirmed) {
            $.ajax({
                url: url,
                type: 'GET',
                success: function (data) {
                    if (data.success) {
                        dataTable.ajax.reload();
                        toastr.success(data.message);

                        //// Re-populate category filter after deletion
                        //setTimeout(function () {
                        //    dataTable.ajax.reload(null, false); // false means don't reset paging
                        //}, 100);
                    }
                    else {
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