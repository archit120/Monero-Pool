$(function(){
    $("[data-load]").each(function(){
        $(this).load($(this).data("load"), function(){
        });
    });

    window.prettyPrint && prettyPrint();

    $(".history-back").on("click", function(e){
        e.preventDefault();
        history.back();
        return false;
    })
})


function headerPosition(){
    
        $("header .navigation-bar")
            .addClass("fixed-top")
            .addClass(" shadow")
        ;
}


$(function(){
    setTimeout(function(){headerPosition();}, 100);
})

METRO_AUTO_REINIT = true;