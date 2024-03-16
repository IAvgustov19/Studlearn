var SwHelpers = function () {
    return {
        getReadableFileSizeString: function (fileSizeInBytes) {

            var i = -1;
            var byteUnits = [' kB', ' MB', ' GB', ' TB', 'PB', 'EB', 'ZB', 'YB'];
            do {
                fileSizeInBytes = fileSizeInBytes / 1024;
                i++;
            } while (fileSizeInBytes > 1024);

            return Math.max(fileSizeInBytes, 0.1).toFixed(1) + byteUnits[i];
        },

        getDateTimeMomentString: function (dateTime) {
            dateTime = moment(moment.utc(dateTime, "YYYY-MM-DDTHH:mm:ss.ttt").toDate()).local("YYYYMMDDHHmmss").fromNow();
            return dateTime;
        },
        getDateTimeString: function (dateTime) {
            dateTime = moment(moment.utc(dateTime, "YYYY-MM-DDTHH:mm:ss.ttt").toDate()).local("YYYYMMDDHHmmss").format("lll");
            return dateTime;
        },
        getDateTimeStringCustom: function (dateTime, format) {
            format = format || "lll";
            dateTime = moment(moment.utc(dateTime, "YYYY-MM-DDTHH:mm:ss.ttt").toDate()).local("YYYYMMDDHHmmss").format(format);
            return dateTime;
        }
    }
}();
