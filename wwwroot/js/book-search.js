$(function () {
    $("#ajaxKeyword").on("keyup", function () {
        const keyword = $(this).val();
        $.ajax({
            url: "/api/books",
            type: "GET",
            data: { keyword },
            success: function (data) {
                let html = "";
                data.forEach(item => {
                    html += `<tr><td>${item.bookCode}</td><td>${item.bookName}</td><td>${item.categoryName}</td><td>${item.authorName}</td><td>${item.availableQuantity}</td></tr>`;
                });
                $("#ajaxBookBody").html(html);
            }
        });
    });
});
