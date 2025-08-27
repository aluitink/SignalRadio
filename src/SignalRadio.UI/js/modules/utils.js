// Utility Functions
export class Utils {
    // Parse dates coming from the server as UTC when no timezone is present.
    // This avoids interpreting an ISO string as local time when the server actually
    // sent a UTC timestamp without a timezone suffix (common source of "Just now").
    parseDateAsUTC(dateString) {
        if (!dateString) return new Date(NaN);

        // If already a Date or a numeric timestamp, return as Date
        if (dateString instanceof Date) return dateString;
        if (typeof dateString === 'number') return new Date(dateString);

        const s = dateString.toString();

        // If string contains a timezone designator (Z) or an offset (+/-HH:MM or +/-HHMM),
        // allow the native Date parser to handle it.
        if (/[zZ]$|[+-]\d{2}:?\d{2}$/.test(s)) {
            return new Date(s);
        }

        // If the string uses a space between date and time, convert to 'T' so it's valid ISO
        let iso = s.replace(' ', 'T');

        // If no timezone info is present, append 'Z' to force parsing as UTC.
        return new Date(iso + 'Z');
    }

    formatDateTime(dateString) {
    const date = this.parseDateAsUTC(dateString);
    if (isNaN(date.getTime())) return '';
    return date.toLocaleString('en-US', {
            month: 'short',
            day: 'numeric',
            hour: '2-digit',
            minute: '2-digit',
            second: '2-digit'
        });
    }

    formatRelativeTime(dateString) {
        const now = new Date();
        const date = this.parseDateAsUTC(dateString);
    if (isNaN(date.getTime())) return 'Unknown';
        const diffMs = now - date;
        const diffSeconds = Math.floor(diffMs / 1000);
        const diffMinutes = Math.floor(diffSeconds / 60);
        const diffHours = Math.floor(diffMinutes / 60);
        const diffDays = Math.floor(diffHours / 24);
        // If the date is slightly in the future (timezone skew, clock skew), show "Just now"
        // for small offsets. For larger future offsets, show a future label.
        if (diffSeconds < 0) {
            const absSeconds = Math.abs(diffSeconds);
            const absMinutes = Math.floor(absSeconds / 60);
            const absHours = Math.floor(absMinutes / 60);
            const absDays = Math.floor(absHours / 24);

            if (absSeconds < 60) return 'Just now';
            if (absMinutes < 60) return `In ${absMinutes}m`;
            if (absHours < 24) return `In ${absHours}h`;
            if (absDays < 7) return `In ${absDays}d`;
            return date.toLocaleDateString();
        }

        if (diffSeconds < 60) {
            return 'Just now';
        } else if (diffMinutes < 60) {
            return `${diffMinutes}m ago`;
        } else if (diffHours < 24) {
            return `${diffHours}h ago`;
        } else if (diffDays < 7) {
            return `${diffDays}d ago`;
        } else {
            return date.toLocaleDateString();
        }
    }

    formatFrequency(frequencyString) {
        // Handle the repeated frequency strings and convert to MHz
        if (!frequencyString) return 'Unknown';
        
        // Clean up repeated frequencies (e.g., "17207500017207500017" -> "172075000")
        let cleanFreq = frequencyString.toString();
        
        // If frequency is very long, it's likely repeated
        if (cleanFreq.length > 12) {
            // Try to find the pattern length by looking for repetition
            for (let i = 6; i <= cleanFreq.length / 2; i++) {
                const pattern = cleanFreq.substring(0, i);
                if (cleanFreq.startsWith(pattern + pattern)) {
                    cleanFreq = pattern;
                    break;
                }
            }
        }
        
        // Convert to proper frequency format
        let freqHz = parseInt(cleanFreq);
        if (isNaN(freqHz)) return frequencyString;
        
        // Radio frequencies are typically 30 MHz to 3000 MHz (30,000,000 Hz to 3,000,000,000 Hz)
        // If number is 8-10 digits, it's likely in Hz
        if (freqHz >= 30000000 && freqHz <= 3000000000) {
            const freqMHz = (freqHz / 1000000).toFixed(5);
            // Keep all 5 decimal places
            return `${freqMHz} MHz`;
        } else {
            // Fallback: display as-is if we can't determine proper format
            return `${cleanFreq} Hz`;
        }
    }

    formatDuration(duration) {
        // Duration comes as "HH:MM:SS" or TimeSpan format
        if (typeof duration === 'string') {
            return duration.split('.')[0]; // Remove milliseconds if present
        }
        return duration;
    }

    formatAudioTime(seconds) {
        const minutes = Math.floor(seconds / 60);
        const remainingSeconds = Math.floor(seconds % 60);
        return `${minutes}:${remainingSeconds.toString().padStart(2, '0')}`;
    }

    getPriorityClass(priority) {
        if (!priority) return null;
        
        if (priority >= 1 && priority <= 3) return 'priority-high';
        if (priority >= 4 && priority <= 6) return 'priority-medium';
        return 'priority-low';
    }

    getPriorityBadgeColor(priority) {
        if (!priority) return 'secondary';
        
        if (priority >= 1 && priority <= 3) return 'danger';
        if (priority >= 4 && priority <= 6) return 'warning';
        return 'success';
    }

    getRecordingQuality(recordings) {
        if (!recordings || recordings.length === 0) return null;
        
        const hasM4A = recordings.some(r => r.fileName && r.fileName.toLowerCase().endsWith('.m4a'));
        const hasWAV = recordings.some(r => r.fileName && r.fileName.toLowerCase().endsWith('.wav'));
        
        if (hasM4A && hasWAV) return 'M4A+WAV';
        if (hasM4A) return 'M4A';
        if (hasWAV) return 'WAV';
        return 'Unknown';
    }

    getAgeClass(dateString) {
        const now = new Date();
    const date = this.parseDateAsUTC(dateString);
    if (isNaN(date.getTime())) return 'age-fresh';
        const diffMs = now - date;
        const diffMinutes = Math.floor(diffMs / (1000 * 60));
        
        if (diffMinutes <= 5) {
            return 'age-fresh';
        } else if (diffMinutes <= 30) {
            return 'age-recent';
        } else if (diffMinutes <= 120) { // 2 hours
            return 'age-medium';
        } else if (diffMinutes <= 1440) { // 24 hours
            return 'age-old';
        } else {
            return 'age-very-old';
        }
    }

    escapeHtml(text) {
        const div = document.createElement('div');
        div.textContent = text;
        return div.innerHTML;
    }

    debounce(func, wait) {
        let timeout;
        return function executedFunction(...args) {
            const later = () => {
                clearTimeout(timeout);
                func(...args);
            };
            clearTimeout(timeout);
            timeout = setTimeout(later, wait);
        };
    }

    throttle(func, limit) {
        let inThrottle;
        return function() {
            const args = arguments;
            const context = this;
            if (!inThrottle) {
                func.apply(context, args);
                inThrottle = true;
                setTimeout(() => inThrottle = false, limit);
            }
        };
    }
}
