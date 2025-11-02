# Script tự động đổi message từ tiếng Anh sang tiếng Việt trong các Controllers
# Chạy script này trong thư mục gốc của project

Write-Host "Bắt đầu đổi message sang tiếng Việt..." -ForegroundColor Green

# Danh sách các cặp Find/Replace
$replacements = @{
    # Request validation
    'Request body is required\.' = 'Yêu cầu phải có dữ liệu đầu vào.'
    'Email is required\.' = 'Email là bắt buộc.'
    'Email and Password are required\.' = 'Email và mật khẩu là bắt buộc.'
    'OTP is required\.' = 'Mã OTP là bắt buộc.'
    'PasswordReset is required\.' = 'Mật khẩu mới là bắt buộc.'
    'FullName is required\.' = 'Họ và tên là bắt buộc.'
    'IdToken is required\.' = 'IdToken là bắt buộc.'
    'Email, Password, CompanyName and CompanyAddress are required\.' = 'Email, mật khẩu, tên tổ chức và địa chỉ tổ chức là bắt buộc.'
    'Email, oldPassword and newPassword are required\.' = 'Email, mật khẩu cũ và mật khẩu mới là bắt buộc.'
    
    # Email related
    'Email does not exist in the system\.' = 'Email không tồn tại trong hệ thống.'
    'Email already exists\. Please use a different email\.' = 'Email đã tồn tại. Vui lòng sử dụng email khác.'
    
    # OTP related
    'OTP has been sent to your email\.' = 'Mã OTP đã được gửi đến email của bạn.'
    'Invalid or expired OTP\.' = 'Mã OTP không hợp lệ hoặc đã hết hạn.'
    'OTP verification successful\. You can now reset your password\.' = 'Xác minh OTP thành công. Bạn có thể đặt lại mật khẩu.'
    
    # Password related
    'Failed to reset password\. Please try again\.' = 'Đặt lại mật khẩu thất bại. Vui lòng thử lại.'
    'Password has been reset successfully\.' = 'Mật khẩu đã được đặt lại thành công.'
    'Failed to change password\. Please try again\.' = 'Đổi mật khẩu thất bại. Vui lòng thử lại.'
    'Password changed successfully\.' = 'Đổi mật khẩu thành công.'
    
    # Login/Auth
    'Invalid email or password\.' = 'Email hoặc mật khẩu không hợp lệ.'
    'A server error prevented login\.' = 'Lỗi máy chủ ngăn không cho đăng nhập.'
    'Your account has been suspended or banned\.' = 'Tài khoản của bạn đã bị đình chỉ hoặc cấm.'
    
    # Registration
    'Registration failed\. Please try again\.' = 'Đăng ký thất bại. Vui lòng thử lại.'
    'Candidate registered successfully\.' = 'Đăng ký học viên thành công.'
    'Recruiter registered successfully\.' = 'Đăng ký giáo viên thành công.'
    'Student registered successfully\.' = 'Đăng ký học viên thành công.'
    
    # Logout
    'Failed to logout\. Please try again\.' = 'Đăng xuất thất bại. Vui lòng thử lại.'
    'Logout successful\.' = 'Đăng xuất thành công.'
    
    # Token
    'Failed to generate a new Access Token\. Please try again\.' = 'Tạo Access Token mới thất bại. Vui lòng thử lại.'
    'Invalid IdToken\.' = 'IdToken không hợp lệ.'
    'Invalid Ip address\.' = 'Địa chỉ IP không hợp lệ.'
    
    # Group operations
    'Group not found' = 'Không tìm thấy nhóm'
    'Student added to group successfully' = 'Thêm học viên vào nhóm thành công'
    'Student is already in the group' = 'Học viên đã có trong nhóm'
    'Error adding student to group' = 'Lỗi khi thêm học viên vào nhóm'
    'Joined group successfully' = 'Tham gia nhóm thành công'
    'Invalid invite code' = 'Mã mời không hợp lệ'
    'Error joining group' = 'Lỗi khi tham gia nhóm'
    'Failed to insert quiz to group' = 'Thêm bài kiểm tra vào nhóm thất bại'
    'Failed to create group' = 'Tạo nhóm thất bại'
    'Group deleted successfully' = 'Xóa nhóm thành công'
    'Left group successfully' = 'Rời nhóm thành công'
    'Group or Student not found' = 'Không tìm thấy nhóm hoặc học viên'
    'Student removed from group successfully' = 'Xóa học viên khỏi nhóm thành công'
    'Quiz removed from group successfully' = 'Xóa bài kiểm tra khỏi nhóm thành công'
    'Group or Quiz not found' = 'Không tìm thấy nhóm hoặc bài kiểm tra'
    
    # Folder operations
    'Folder created successfully' = 'Tạo thư mục thành công'
    'Failed to create Folder' = 'Tạo thư mục thất bại'
    'No folders found for this teacher' = 'Không tìm thấy thư mục nào của giáo viên này'
    'No quizz found for this teacher or folder' = 'Không tìm thấy bài kiểm tra nào của giáo viên hoặc thư mục này'
    
    # Admin operations
    'Page and PageSize must be greater than 0\.' = 'Trang và kích thước trang phải lớn hơn 0.'
    'Month must be between 1 and 12' = 'Tháng phải từ 1 đến 12'
    'Invalid account ID' = 'ID tài khoản không hợp lệ'
    'Account banned successfully' = 'Cấm tài khoản thành công'
    'Account not found or already banned' = 'Không tìm thấy tài khoản hoặc đã bị cấm'
    'Account unbanned successfully' = 'Bỏ cấm tài khoản thành công'
    'Account not found or already active' = 'Không tìm thấy tài khoản hoặc đã hoạt động'
    
    # Profile operations
    'Student profile not found' = 'Không tìm thấy hồ sơ học viên'
    'Get student profile successfully' = 'Lấy hồ sơ học viên thành công'
    'Failed to update profile' = 'Cập nhật hồ sơ thất bại'
    'Update student profile successfully' = 'Cập nhật hồ sơ học viên thành công'
    'Teacher profile not found' = 'Không tìm thấy hồ sơ giáo viên'
    'Get teacher profile successfully' = 'Lấy hồ sơ giáo viên thành công'
    'Update teacher profile successfully' = 'Cập nhật hồ sơ giáo viên thành công'
    
    # Quiz operations
    'Quiz not found\.' = 'Không tìm thấy bài kiểm tra.'
    'No quizzes found\.' = 'Không tìm thấy bài kiểm tra nào.'
    'Quiz deleted successfully\.' = 'Xóa bài kiểm tra thành công.'
    'Quiz not found or could not be deleted\.' = 'Không tìm thấy bài kiểm tra hoặc không thể xóa.'
    'Question deleted successfully\.' = 'Xóa câu hỏi thành công.'
    'Question not found or could not be deleted\.' = 'Không tìm thấy câu hỏi hoặc không thể xóa.'
    'An error occurred while creating the quiz\.' = 'Đã xảy ra lỗi khi tạo bài kiểm tra.'
    'An error occurred while updating the quiz\.' = 'Đã xảy ra lỗi khi cập nhật bài kiểm tra.'
    'No correct answers found for the provided question IDs\.' = 'Không tìm thấy câu trả lời đúng cho các ID câu hỏi được cung cấp.'
    
    # Report operations
    'Invalid teacher ID' = 'ID giáo viên không hợp lệ'
    'Invalid offline report ID or quiz ID' = 'ID báo cáo offline hoặc ID bài kiểm tra không hợp lệ'
    'Offline report not found' = 'Không tìm thấy báo cáo offline'
    'Invalid quiz ID, QG ID, or group ID' = 'ID bài kiểm tra, QG ID hoặc ID nhóm không hợp lệ'
    'Invalid quiz ID or online report ID' = 'ID bài kiểm tra hoặc ID báo cáo online không hợp lệ'
    'Online report not found' = 'Không tìm thấy báo cáo online'
    'Quiz has expired and status updated' = 'Bài kiểm tra đã hết hạn và trạng thái đã được cập nhật'
    'Quiz is still active' = 'Bài kiểm tra vẫn còn hoạt động'
    'Quiz ended successfully' = 'Kết thúc bài kiểm tra thành công'
    'Failed to end quiz' = 'Kết thúc bài kiểm tra thất bại'
    'Expired time updated successfully' = 'Cập nhật thời gian hết hạn thành công'
    'Quiz group not found' = 'Không tìm thấy nhóm bài kiểm tra'
    'Expired time must be in the future' = 'Thời gian hết hạn phải là thời gian tương lai'
    'Failed to update expired time' = 'Cập nhật thời gian hết hạn thất bại'
    'Invalid offline report ID' = 'ID báo cáo offline không hợp lệ'
    'Report name cannot be empty' = 'Tên báo cáo không được để trống'
    'Offline report name updated successfully' = 'Cập nhật tên báo cáo offline thành công'
    'Offline report not found or update failed' = 'Không tìm thấy báo cáo offline hoặc cập nhật thất bại'
    'Invalid online report ID' = 'ID báo cáo online không hợp lệ'
    'Online report name updated successfully' = 'Cập nhật tên báo cáo online thành công'
    'Online report not found or update failed' = 'Không tìm thấy báo cáo online hoặc cập nhật thất bại'
    'Invalid question ID' = 'ID câu hỏi không hợp lệ'
    'Question not found' = 'Không tìm thấy câu hỏi'
    
    # Student report
    'Invalid student ID' = 'ID học viên không hợp lệ'
    'Quiz detail not found' = 'Không tìm thấy chi tiết bài kiểm tra'
    'Quiz result not found for the specified parameters' = 'Không tìm thấy kết quả bài kiểm tra với các tham số được chỉ định'
    'CreateAt query parameter is required and must be a valid date' = 'Tham số truy vấn CreateAt là bắt buộc và phải là ngày hợp lệ'
    
    # General errors
    'Internal server error' = 'Lỗi máy chủ nội bộ'
    'An error occurred while processing your request\.' = 'Đã xảy ra lỗi khi xử lý yêu cầu của bạn.'
    'An unexpected error occurred\.' = 'Đã xảy ra lỗi không mong muốn.'
    'Unknown error' = 'Lỗi không xác định'
    
    # Connection
    'Healthy' = 'Kết nối tốt'
    'Unhealthy' = 'Kết nối thất bại'
}

# Lấy tất cả file Controllers
$controllerFiles = Get-ChildItem -Path "Controllers" -Filter "*.cs" -Recurse

$totalReplacements = 0

foreach ($file in $controllerFiles) {
    Write-Host "Đang xử lý: $($file.Name)" -ForegroundColor Yellow
    
    $content = Get-Content $file.FullName -Raw -Encoding UTF8
    $originalContent = $content
    
    foreach ($key in $replacements.Keys) {
        $pattern = '(message\s*[=:]\s*")' + $key + '(")'
        $replacement = '${1}' + $replacements[$key] + '${2}'
        $content = $content -replace $pattern, $replacement
        
        # Cho trường hợp không có "message ="
        $pattern2 = '("' + $key + '")'
        $replacement2 = '"' + $replacements[$key] + '"'
        if ($content -match $pattern2) {
            $content = $content -replace $pattern2, $replacement2
        }
    }
    
    if ($content -ne $originalContent) {
        Set-Content -Path $file.FullName -Value $content -Encoding UTF8 -NoNewline
        $totalReplacements++
        Write-Host "  ✓ Đã cập nhật" -ForegroundColor Green
    } else {
        Write-Host "  - Không có thay đổi" -ForegroundColor Gray
    }
}

Write-Host "`n=====================================" -ForegroundColor Cyan
Write-Host "Hoàn thành! Đã cập nhật $totalReplacements file(s)" -ForegroundColor Green
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host "`nLưu ý: Hãy kiểm tra lại code và chạy build để đảm bảo không có lỗi." -ForegroundColor Yellow
