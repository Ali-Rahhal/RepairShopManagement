var dataTable;

function loadCategories() {
    $.get('/Admin/Parts/Index?handler=Categories', function (categories) {
        populateCategoryFilter(categories);
    });
}

$(function () {
    loadCategories();   // ✅ load once
    loadDataTable();
});

function loadDataTable() {
    dataTable = $('#tblData').DataTable({
        serverSide: true,            // ✅ server-side
        processing: true,
        stateSave: true,
        stateDuration: 86400,
        order: [[1, 'asc']], // default: Category ascending
        ajax: {
            url: '/Admin/Parts/Index?handler=All',
            type: 'GET',
            data: function (d) {
                d.category = $('#categoryFilter').val() || 'All';
            },
            error: function () {
                toastr.error('Failed to load parts');
            }
        },
        "dom": '<"d-flex justify-content-between align-items-center mb-2"l<"ml-auto"f>>rtip',
        "columns": [
            { data: 'name', "width": "35%" },
            { data: 'category', "width": "25%" },
            { data: 'quantity', "width": "10%" },
            { data: 'price', "width": "10%" },
            {
                data: 'id',
                "render": function (data) {
                    return `<div class="w-100 d-flex justify-content-center" role="group">
                        <a href="/Admin/Parts/Upsert?id=${data}" title="Edit" class="btn btn-primary mx-2"><i class="bi bi-pencil-square"></i></a>
                        <a onClick="Delete('/Admin/Parts/Index?handler=Delete&id=${data}')" title="Delete" class="btn btn-danger mx-2"><i class="bi bi-trash-fill"></i></a>
                    </div>`;
                },
                "width": "20%",
                "orderable": false
            },
        ],
        "language": {
            "emptyTable": "No parts found",
            "zeroRecords": "No matching parts found"
        }
    });

    // Add event listener for category filter
    $('#categoryFilter').on('change', function () {
        applyCategoryFilter();
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
        select.append(`<option value="${category}">${category}</option>`);
    });
}

function applyCategoryFilter() {
    dataTable.ajax.reload(); // server handles filtering
}

function clearCategoryFilter() {
    $('#categoryFilter').val('All');
    dataTable.order([[1, 'asc']]).ajax.reload();
    toastr.info('Category filter cleared');
}

function Delete(url) {
    Swal.fire({
        title: "Are you sure you want to delete this replacement part?",
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
