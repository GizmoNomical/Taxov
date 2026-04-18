redirectToCheckout = function (sessionId)
{
    var stripe = Stripe('pk_test_h8scrx6L7yoJNy0yutdQ9EvC00ZSOXuJcv');
    stripe.redirectToCheckout({
        sessionId: sessionId
    })
}

//RSS Fade Text
function RSSFadeout()
{

    var fade = document.getElementById("RSSElementA");

    var intervalID = setInterval(function () {

        //if (!fade.style.opacity) {
        //    fade.style.opacity = 1;
        //}

        if (fade.style.opacity > 0) {
            fade.style.opacity -= 0.012;
        }
        else
        {
            clearInterval(intervalID);
            return;
        }


    }, 50);
}

function RSSResetOpacity()
{
    var fade = document.getElementById("RSSElementA");

    fade.style.opacity = 1;
}



function collapseChart(id)
{    
    try {
        var chart = document.getElementById(id)
        if (chart === null)
        {
            return;
        }
        else
        {
            chart.classList.toggle("active");
            var content = chart.nextElementSibling;
            if (content.style.maxHeight)
            {
                content.style.maxHeight = null;
            }
            else
            {
                content.style.maxHeight = content.scrollHeight + "px";
            }
        }
    }
    catch
    {        
        collapseChart(id);
    }    
}

function previewBoltAction()
{

// Get the modal
var modal = document.getElementById("previewBox1");

// Get the <span> element that closes the modal
var span = document.getElementsByClassName("close")[0];

// When the user clicks the button, open the modal 

    modal.style.display = "block";


// When the user clicks on <span> (x), close the modal
span.onclick = function () {
    modal.style.display = "none";
}

// When the user clicks anywhere outside of the modal, close it
window.onclick = function (event) {
    if (event.target == modal) {
        modal.style.display = "none";
    }
}
}


function previewSemiAuto() {

    // Get the modal
    var modal = document.getElementById("previewBox2");

    // Get the <span> element that closes the modal
    var span = document.getElementsByClassName("close")[1];

    // When the user clicks the button, open the modal 

    modal.style.display = "block";


    // When the user clicks on <span> (x), close the modal
    span.onclick = function () {
        modal.style.display = "none";
    }

    // When the user clicks anywhere outside of the modal, close it
    window.onclick = function (event) {
        if (event.target == modal) {
            modal.style.display = "none";
        }
    }
}


function previewFullAuto() {

    // Get the modal
    var modal = document.getElementById("previewBox3");

    // Get the <span> element that closes the modal
    var span = document.getElementsByClassName("close")[2];

    // When the user clicks the button, open the modal 

    modal.style.display = "block";


    // When the user clicks on <span> (x), close the modal
    span.onclick = function () {
        modal.style.display = "none";
    }

    // When the user clicks anywhere outside of the modal, close it
    window.onclick = function (event) {
        if (event.target == modal) {
            modal.style.display = "none";
        }
    }
}

function previewFullAutoSubsPage() {

    // Get the modal
    var modal = document.getElementById("previewBox3");

    // Get the <span> element that closes the modal
    //var span = document.getElementByClassName("close");
    var span = document.getElementById("closeButton");

    // When the user clicks the button, open the modal 

    modal.style.display = "block";


    // When the user clicks on <span> (x), close the modal
    span.onclick = function () {
        modal.style.display = "none";
    }

    // When the user clicks anywhere outside of the modal, close it
    window.onclick = function (event) {
        if (event.target == modal) {
            modal.style.display = "none";
        }
    }
}






//test mobile menu code
//function openNav() {
//    document.getElementById("mySidenav").style.width = "100%";
//}
//
//function closeNav() {
//    document.getElementById("mySidenav").style.width = "0";
//}
//function openTools() {
//    document.getElementById("Subnav").style.width = "100%";
//}
//
//function closeTools() {
//    document.getElementById("Subnav").style.width = "0";
//}
  