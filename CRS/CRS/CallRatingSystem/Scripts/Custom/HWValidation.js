class Validation {
	ErrorLable;
    Fields = [];
}

class Fields {
    constructor(_control, _type, _name) {
        this.control = _control;
        this.type = _type;
        this.name = _name;
    }
	control;
	type;
	name;
}

function ValidateForm(form) {

    var allValid = true;
    var error = "";
    for (let field of form.Fields) {
        var result = ValidateControl(field.control, field.type, field.name);

        if (result.valid == false) {
            error += result.error;
            allValid = false;
        }
    }

    if (allValid == false) {
        form.ErrorLable.html(error);
        form.ErrorLable.css("display", "block");
    } 

    return allValid;
}    




function ValidateControl(control, type, name) {

    control.removeClass("is-invalid");

    if (type == "Text") {
        if (control.val() == "") {
            control.addClass("is-invalid");
            return {
                valid: false, error: "-Please enter a value for <b>" + name + "</b> field</br>"
            };
        }
    }

    if (type == "Dropdown") {
        if (control.val() == "0") {
            control.addClass("is-invalid");
            return {
                valid: false, error: "-Please select a value for <b>" + name + "</b> field</br>"
            };
        }
    }

    if (type == "Password") {
        //(?=.*[!@#\$%\^&\*])
        var strongRegex = new RegExp("^(?=.*[a-z])(?=.*[A-Z])(?=.*[0-9])(?=.{8,})")

        if (!strongRegex.test(control.val())) {
            control.addClass("is-invalid");
            return {
                valid: false, error: "-Please enter secure password with one lower case, one upercase and atleast 8 chars for <b>" + name + "</b> field</br>"
            };
        }
    }

    if (type == "Email") {
        if (!/^([a-zA-Z0-9_.+-])+\@(([a-zA-Z0-9-])+\.)+([a-zA-Z0-9]{2,4})+$/.test(control.val())) {
            control.addClass("is-invalid");
            return {
                valid: false, error: "-Please enter valid email address <b>" + name + "</b> field</br>"
            };
        }
    }

    if (type == "Mobile") {
        if (!/^(\+\d{1,3}[- ]?)?\d{10}$/.test(control.val())) {
            control.addClass("is-invalid");
            return {
                valid: false, error: "-Please enter valid mobile number <b>" + name + "</b> field</br>"
            };
        }
    }

    if (type == "Decimal") {
        if (!$.isNumeric(control.val())) {
            control.addClass("is-invalid");
            return {
                valid: false, error: "-Please enter valid number using <b>" + name + "</b> field</br>"
            }
        }
    }

    if (type == "DateToday") {

        var GivenDate = control.val();
        var CurrentDate = new Date();
        GivenDate = new Date(GivenDate);

        if (GivenDate > CurrentDate) {

            control.addClass("is-invalid");
            return {
                valid: false, error: "-Please enter a date less than or equal today's date <b>" + name + "</b> field</br>"
            }
        }
    }

    if (type == "Qty") {

        var Qty = control.val();

        if (Qty < 1) {

            control.addClass("is-invalid");
            return {
                valid: false, error: "-Please enter a value greater than 0 <b>" + name + "</b> field</br>"
            }
        }
    }

    if (type == "InvalidValue") {

        control.addClass("is-invalid");
        return {
            valid: false, error: "-Please enter correct value <b>" + name + "</b> field</br>"
        }
    }

    return { valid: true, error: "" };
}

function ValidateControlOld(control, type) {

    control.removeClass("is-invalid");

    if (type == "Text") {
        if (control.val() == "") {
            control.addClass("is-invalid");
            return false;
        }
    }

    if (type == "Dropdown") {
        if (control.val() == "0") {
            control.addClass("is-invalid");
            return false;
        }
    }

    if (type == "Password") {
        //(?=.*[!@#\$%\^&\*])
        var strongRegex = new RegExp("^(?=.*[a-z])(?=.*[A-Z])(?=.*[0-9])(?=.{8,})")

        if (!strongRegex.test(control.val())) {
            control.addClass("is-invalid");
            return false;
        }
    }

    if (type == "Email") {
        if (!/^([a-zA-Z0-9_.+-])+\@(([a-zA-Z0-9-])+\.)+([a-zA-Z0-9]{2,4})+$/.test(control.val())) {
            control.addClass("is-invalid");
            return false;
        }
    }

    if (type == "Mobile") {
        if (!/^(\+\d{1,3}[- ]?)?\d{10}$/.test(control.val())) {
            control.addClass("is-invalid");
            return false;
        }
    }

    if (type == "Decimal") {
        if (!$.isNumeric(control.val())) {
            control.addClass("is-invalid");
            return false;
        }
    }

    return true;
}