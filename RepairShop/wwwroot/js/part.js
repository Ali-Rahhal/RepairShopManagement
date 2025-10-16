var dataTable;

$(document).ready(function () {
    loadDataTable();
});

function loadDataTable() {
    dataTable = $('#tblData').DataTable({
        "stateSave": true,
        "stateDuration": 86400, // Any positive number = sessionStorage (in seconds)
        // 86400 seconds = 24 hours, but sessionStorage lasts only for the browser session
        "ajax": {
            url: '/Admin/Parts/Index?handler=All',
            dataSrc: function (json) {
                // Extract unique categories for the filter
                var categories = [...new Set(json.data.map(item => item.category || 'Uncategorized'))];
                populateCategoryFilter(categories);
                return json.data;
            }
        },
        "dom": '<"d-flex justify-content-between align-items-center mb-2"l<"ml-auto"f>>rtip',
        "columns": [
            { data: 'name', "width": "25%" },
            { data: 'category', "width": "20%" },
            { data: 'quantity', "width": "15%" },
            { data: 'price', "width": "15%" },
            {
                data: 'id',
                "render": function (data) {
                    return `<div class="w-100 d-flex justify-content-center" role="group">
                        <a href="/Admin/Parts/Upsert?id=${data}" title="Edit" class="btn btn-primary mx-2"><i class="bi bi-pencil-square"></i></a>
                        <a onClick="Delete('/Admin/Parts/Index?handler=Delete&id=${data}')" title="Delete" class="btn btn-danger mx-2"><i class="bi bi-trash-fill"></i></a>
                    </div>`;
                },
                "width": "25%"
            },
        ],
        order: [[1, 'asc']],
        "language": {
            "emptyTable": "No parts found",
            "zeroRecords": "No matching parts found"
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
    dataTable.order([[1, 'asc']]).draw();
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
